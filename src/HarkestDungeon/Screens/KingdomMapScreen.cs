using System;
using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Data;
using Assets.Code.Kingdom;
using Assets.Code.Kingdom.Events;
using Assets.Code.Kingdom.Presentation;
using Assets.Code.Quirk;
using Assets.Code.UI.Kingdom;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The kingdoms overworld map (open whenever the presentation says so and no screen covers
    /// it). First element: the grid cursor - arrows step cell by cell, Home returns to the
    /// stagecoach, Enter activates through the game's own cell activation (inn/biome panels,
    /// boss travel), and every landing mirrors into the game's own selection so the camera
    /// follows. A cell reads only what its own view displays: name, stagecoach/travel state,
    /// stationed heroes, siege with its banded strength, treasure, boss - a hidden boss ring
    /// reads as empty ground because its view objects are inactive. Then the day readout with
    /// the pass-day button and current event, the escalation readout, the timeline's marked
    /// days, the sidebar (one element per active siege - Enter jumps the cursor there - and the
    /// cursed-regions counter), the hero row, and the footer's sheet and inventory buttons.
    /// A hero row reads name, class, where the hero is (the party or their inn), a curse, and
    /// a scheduled trip's destination. Enter on a party hero jumps the cursor to the party;
    /// on a stationed hero it enters the game's own hero-travel mode: the cursor lands on the
    /// hero's inn, every inn in range reads as a destination with the route's length, Enter on
    /// one commits the trip and Escape cancels the mode, both landing back on the hero's row.
    /// Escape otherwise closes through CloseMap, which the game refuses mid day-turn.
    /// </summary>
    public sealed class KingdomMapScreen : GameScreen {
        private static readonly AccessTools.FieldRef<KingdomMapCellInnInfoBhv, int> MedSiegeThresholdField =
            AccessTools.FieldRefAccess<KingdomMapCellInnInfoBhv, int>("m_medStrengthSiegeThreshold");
        private static readonly AccessTools.FieldRef<KingdomMapCellInnInfoBhv, int> HighSiegeThresholdField =
            AccessTools.FieldRefAccess<KingdomMapCellInnInfoBhv, int>("m_highStrengthSiegeThreshold");

        private readonly TraditionalNavigator _navigator;
        private readonly Action<string, bool> _speak;
        private readonly Action _cursorMoved;

        private Vector2Int _cursor;
        private bool _sessionLive;
        private UIElement _cursorElement;
        private Container _root;
        private Container _heroes;
        private int _builtSignature;

        public KingdomMapScreen(TraditionalNavigator navigator, Action<string, bool> speak, Action cursorMoved) {
            _navigator = navigator;
            _speak = speak;
            _cursorMoved = cursorMoved;
        }

        public override string Name => S.ScreenKingdomMap;

        public override object ResolveTarget() {
            if (!SingletonMonoBehaviour<KingdomBhv>.HasInstance()) {
                return null;
            }
            // The game's own KingdomBhv.IsMapOpen dereferences the map scene's presentation
            // without a null check and throws while the scene is still loading in; this walks
            // the same chain guarded.
            var mapManager = SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomMapManager;
            var mapRoot = mapManager == null || !mapManager.IsMapLoaded ? null : mapManager.KingdomMapRoot;
            var presentation = mapRoot == null ? null : mapRoot.GetKingdomPresentationFromScene();
            if (presentation == null || !presentation.IsMapOpen) {
                return null;
            }
            // Any pushed screen (a cell panel, the hero sheet) covers the map and reads instead.
            if (SingletonMonoBehaviour<ScreenStackBhv>.HasInstance()) {
                var top = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
                if (top != null && top.m_screenType == ScreenStackBhv.ScreenOrderType.SCREEN && top.m_screenObj != null) {
                    return null;
                }
            }
            return presentation;
        }

        public override Container BuildRoot(object target) {
            // The cursor survives a panel round-trip (the map stayed open behind it) and homes
            // to the stagecoach on a fresh open.
            if (!_sessionLive || !Manager().KingdomMap.IsValidCoordinate(_cursor)) {
                _cursor = Manager().CurrentStageCoachCoordinates;
            }
            _sessionLive = true;
            // A Panel root so Tab crosses cursor / header / sidebar / heroes / footer - the
            // cursor element owns the arrows while focused, so Tab is the only way off it - and
            // Tab wraps, so the cursor is always a few presses away.
            _root = new RootContainer(ContainerShape.Panel, back: () => {
                var presentation = (KingdomPresentationBhv)target;
                if (!presentation.CloseMap()) {
                    _speak(S.StatusUnavailable, true);
                }
            });
            _root.WrapTabStops = true;
            Populate();
            MirrorSelection();
            return _root;
        }

        public override bool OnUpdate(object target) {
            if (Signature() != _builtSignature) {
                _root.Clear();
                Populate();
            }
            return false;
        }

        public override void OnLeave() {
            if (!SingletonMonoBehaviour<KingdomBhv>.HasInstance() || !KingdomBhv.IsMapOpen()) {
                _sessionLive = false;
            }
        }

        public override bool HandleAction(string actionKey) {
            // The first stage of the game's own back press: leave hero-travel mode.
            if (actionKey == UiActions.Back && CancelHeroMove()) {
                return true;
            }
            if (_navigator.Current != _cursorElement) {
                return false;
            }
            switch (actionKey) {
                case UiActions.Up: return Step(0, -1);
                case UiActions.Down: return Step(0, 1);
                case UiActions.Left: return Step(-1, 0);
                case UiActions.Right: return Step(1, 0);
                case UiActions.Home: return JumpTo(Manager().CurrentStageCoachCoordinates);
                default: return false;
            }
        }

        // ---- Grid cursor ----

        private static KingdomMapManager Manager() =>
            SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomMapManager;

        private static Assets.Code.Roster.RosterManager Roster() =>
            Singleton<Assets.Code.Game.GameTypeMgr>.Instance.RosterManager;

        private bool Step(int dx, int dy) {
            var map = Manager().KingdomMap;
            var next = new Vector2Int(_cursor.x + dx, _cursor.y + dy);
            if (!map.IsValidCoordinate(next)) {
                _speak(CursorLine(), true);
                return true;
            }
            return JumpTo(next);
        }

        private bool JumpTo(Vector2Int coords) {
            _cursor = coords;
            MirrorSelection();
            _speak(CursorLine(), true);
            _cursorMoved();
            return true;
        }

        // The game's own selection follows the cursor (camera pan, highlight, travel-arrow
        // preview - all presentational) whenever the cell is currently selectable; hidden or
        // restricted cells are still readable, just not selected.
        private void MirrorSelection() {
            var view = Manager().KingdomMapRoot[_cursor] as KingdomMapCellBhv;
            if (view == null || !view.gameObject.activeInHierarchy) {
                return;
            }
            var selectable = view.GetComponent<Selectable>();
            if (selectable != null && selectable.interactable) {
                view.Select();
            }
        }

        private KingdomMapCellBase Cell() => Manager().KingdomMap.TryGetCell(_cursor);

        // A cell whose view is inactive shows the player nothing (the pre-reveal boss ring
        // hides this way), so it reads as empty ground regardless of what the model knows.
        private KingdomMapCellBhv VisibleView() {
            var view = Manager().KingdomMapRoot[_cursor] as KingdomMapCellBhv;
            return view != null && view.gameObject.activeInHierarchy ? view : null;
        }

        private string CursorLine() {
            var mgr = Manager();
            var cell = Cell();
            var view = VisibleView();
            if (cell == null || cell.CellType == null || view == null) {
                return SpokenLine.Join(S.PanelEmpty, PositionWords());
            }
            var context = view.GetComponentInChildren<DataContextBhv>(includeInactive: false);
            var parts = new List<string>();
            string name = context == null ? null : context.GetStringValue("cell_name");
            if (cell is KingdomMapCellBoss) {
                name = SpokenLine.Join(S.KingdomBoss, name);
            }
            parts.Add(string.IsNullOrEmpty(name) ? S.PanelEmpty : name);
            uint moving = mgr.GetSelectedTransferActorGuid();
            if (moving != 0 && mgr.AreCoordinatesValidForActorTransfer(moving, _cursor)) {
                // The game calls out every inn the moving hero may reach and draws the route
                // onto the selected one; its length in regions decides the trip's risk.
                parts.Add(S.KingdomDestination);
                int regions = RegionsBetween(mgr, moving, _cursor);
                if (regions > 0) {
                    parts.Add(S.KingdomRegionsAway(regions));
                }
            }
            if (_cursor == mgr.CurrentStageCoachCoordinates) {
                parts.Add(S.KingdomStagecoach);
            } else if (mgr.HasNextStageCoachCoordinates && _cursor == mgr.NextStageCoachCoordinates) {
                parts.Add(S.KingdomTravelScheduled);
            } else if (moving == 0 && Reachable(mgr)) {
                parts.Add(S.KingdomReachable);
            }
            foreach (uint guid in cell.ActorGuids) {
                if (guid != 0) {
                    var actor = Actors.Get(guid);
                    if (actor != null) {
                        parts.Add(Actors.Name(actor));
                    }
                }
            }
            parts.Add(SiegeWords(cell));
            if (context != null && context.GetBoolValue("treasure_active")) {
                parts.Add(SpokenLine.Join(S.KingdomTreasure, context.GetStringValue("treasure_duration")));
            }
            if (cell is KingdomMapCellBiome biome) {
                if (biome.BiomeModifier != null) {
                    parts.Add(biome.BiomeModifier.Tags.ContainsAll(new[] { "infection", "courtier" })
                        ? S.KingdomCursed : S.KingdomQuest);
                }
                if (biome.GetHasKillContract()) {
                    parts.Add(GameLoc.TryGet("kill_contract_title_" + ContractId(biome)));
                }
                if (biome.BiomeReward != null) {
                    parts.Add(S.KingdomReward);
                }
                if (biome.BiomeUpgradeInstances.Count > 0) {
                    parts.Add(S.KingdomUpgraded);
                }
            }
            return SpokenLine.Join(parts.ToArray());
        }

        private bool Reachable(KingdomMapManager mgr) {
            if (!SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomDaySystem.IsInUserInputState()) {
                return false;
            }
            return mgr.AreCoordinatesValidForStagecoachTravel(_cursor)
                || mgr.AreCoordinatesValidForStagecoachFastTravel(_cursor);
        }

        // Inns sit two grid steps apart with a region between, the unit the game's travel
        // rule counts ("1 region: risk free, 2 regions: fatigue").
        private static int RegionsBetween(KingdomMapManager mgr, uint guid, Vector2Int destination) {
            var origin = mgr.KingdomMap.TryGetCell(guid);
            if (origin == null) {
                return 0;
            }
            var path = new List<Vector2Int>();
            if (!KingdomMapPathfinding.FindPath(mgr.KingdomMap, origin.Coordinates, destination, path)) {
                return 0;
            }
            return (path.Count - 1) / 2;
        }

        // The cell icon buckets siege strength into three visual bands; the exact number is
        // never shown, so only the band is spoken.
        private string SiegeWords(KingdomMapCellBase cell) {
            var sieges = SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomSiegeManager;
            if (!sieges.GetIsSiegeAtCoordinates(cell.Coordinates)) {
                return null;
            }
            KingdomSiegeInstance siege = null;
            foreach (var instance in sieges.KingdomSiegeInstances) {
                if (instance.m_Coordinates == cell.Coordinates) {
                    siege = instance;
                    break;
                }
            }
            if (siege == null) {
                return null;
            }
            string band = null;
            var info = VisibleView() == null ? null : VisibleView().GetComponentInChildren<KingdomMapCellInnInfoBhv>(includeInactive: false);
            if (info != null) {
                int med = MedSiegeThresholdField(info);
                int high = HighSiegeThresholdField(info);
                if (siege.Strength >= high) {
                    band = S.KingdomSiegeHigh;
                } else if (siege.Strength >= med) {
                    band = S.KingdomSiegeMedium;
                }
            }
            string delay = string.Format(GameLoc.TryGet("kingdom_map_days_remaining") ?? "{0}", siege.Delay);
            return SpokenLine.Join(S.KingdomSiege, band, delay);
        }

        private static string ContractId(KingdomMapCellBiome biome) {
            var contract = Actors.Get(biome.BiomeKillContractGuid);
            return contract == null ? null : contract.ActorDataId;
        }

        private string PositionWords() => S.KingdomCell(_cursor.y + 1, _cursor.x + 1);

        private IEnumerable<string> CursorDetail() {
            yield return CursorLine();
            var view = VisibleView();
            if (view != null) {
                foreach (var line in TooltipReader.Lines(view.gameObject)) {
                    yield return line;
                }
                var context = view.GetComponentInChildren<DataContextBhv>(includeInactive: false);
                string fastTravel = context == null ? null : GameLoc.TryGet(context.GetStringValue("fast_travel_label"));
                if (!string.IsNullOrEmpty(fastTravel)) {
                    yield return fastTravel;
                }
            }
            var cell = Cell();
            foreach (var line in HeroDetail(cell)) {
                yield return line;
            }
            yield return PositionWords();
        }

        private IEnumerable<string> HeroDetail(KingdomMapCellBase cell) {
            if (cell == null) {
                yield break;
            }
            foreach (uint guid in cell.ActorGuids) {
                if (guid == 0) {
                    continue;
                }
                var actor = Actors.Get(guid);
                if (actor != null) {
                    yield return SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id));
                }
            }
        }

        private void ActivateCell() {
            var kingdom = SingletonMonoBehaviour<KingdomBhv>.Instance;
            var presentation = UnityEngine.Object.FindObjectOfType<KingdomPresentationBhv>();
            if (!kingdom.KingdomDaySystem.IsInUserInputState()
                || (presentation != null && presentation.IsAnimating)) {
                _speak(S.StatusUnavailable, true);
                return;
            }
            var cell = Cell();
            if (cell == null || cell.CellType == null || VisibleView() == null) {
                _speak(S.StatusUnavailable, true);
                return;
            }
            uint moving = kingdom.KingdomMapManager.GetSelectedTransferActorGuid();
            if (moving != 0) {
                // In hero-travel mode the game answers only an inn in range (every other cell
                // is ignored, panels included); the commit hands focus back to the hero's row,
                // which now reads the trip.
                if (!kingdom.KingdomMapManager.AreCoordinatesValidForActorTransfer(moving, _cursor)) {
                    _speak(S.StatusUnavailable, true);
                    return;
                }
                EventKingdomActivateMapCell.Trigger(_cursor, wasSelectedByPointer: false);
                LandOnHero(moving);
                return;
            }
            EventKingdomActivateMapCell.Trigger(_cursor, wasSelectedByPointer: false);
        }

        // ---- Hero travel ----

        private void ActivateHero(HeroElement element) {
            var mgr = Manager();
            uint guid = element.Guid;
            if (Roster().GetIsActorInParty(guid)) {
                // A party hero travels with the coach; the game's click pans the camera to
                // the party, and the cursor is the mod's camera.
                JumpTo(mgr.CurrentStageCoachCoordinates);
                _navigator.Focus(_cursorElement, announce: false);
                return;
            }
            bool wasMoving = mgr.GetSelectedTransferActorGuid() == guid;
            element.PressGame();
            if (mgr.GetSelectedTransferActorGuid() == guid) {
                // The game parks its selection on the hero's own inn; the cursor follows.
                var cell = mgr.KingdomMap.TryGetCell(guid);
                if (cell != null) {
                    _cursor = cell.Coordinates;
                    MirrorSelection();
                }
                _navigator.Focus(_cursorElement, announce: false);
                _speak(S.KingdomMovingHero(Actors.Name(Actors.Get(guid))), true);
                _speak(CursorLine(), false);
                _cursorMoved();
            } else if (wasMoving) {
                // The same press again leaves the mode; the row reads plain again.
                _speak(element.GetFocusText(), true);
            } else {
                // An immobile hero, or a day turn in progress: the game's own press refuses
                // silently.
                _speak(S.StatusUnavailable, true);
            }
        }

        private bool CancelHeroMove() {
            var mgr = Manager();
            uint guid = mgr.GetSelectedTransferActorGuid();
            if (guid == 0) {
                return false;
            }
            mgr.ClearSelectedTransferActor(isCancel: true);
            LandOnHero(guid);
            return true;
        }

        // Focus returns to the hero's own row, whose read shows the move's outcome.
        private void LandOnHero(uint guid) {
            var element = HeroElementOf(guid);
            if (element != null) {
                _navigator.Focus(element, announce: true);
            } else {
                _speak(CursorLine(), true);
            }
        }

        private HeroElement HeroElementOf(uint guid) {
            if (_heroes == null) {
                return null;
            }
            foreach (var child in _heroes.Children) {
                if (child is HeroElement hero && hero.Guid == guid) {
                    return hero;
                }
            }
            return null;
        }

        private string HeroHeaderLine(KingdomMapActorHeaderButtonBhv button) {
            var actor = Actors.Get(button.ActorGuid);
            if (actor == null) {
                return UiText.FirstLabel(button.gameObject);
            }
            string where;
            if (Roster().GetIsActorInParty(button.ActorGuid)) {
                where = S.CrossroadsInParty;
            } else {
                var cell = Manager().KingdomMap.TryGetCell(button.ActorGuid);
                where = cell == null ? null : InnNameAt(cell.Coordinates);
            }
            return SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id),
                where, CurseName(actor), TravelLine(button.ActorGuid));
        }

        // The header's travelling icon, named by the inn panel's own caption for the trip.
        private string TravelLine(uint guid) {
            if (!Manager().TryGetTransferCoordsForActor(guid, out var destination)) {
                return null;
            }
            string inn = InnNameAt(destination);
            string template = GameLoc.TryGet("kingdom_inn_panel_travel_tooltip_label");
            return template == null ? SpokenLine.Join(S.KingdomTravelScheduled, inn) : string.Format(template, inn);
        }

        // The header's curse badge (the quirk the game tags as a curse - the Crimson Curse a
        // boss visit may demand), by the quirk's own name.
        private static string CurseName(ActorInstance actor) {
            if (!actor.QuirkContainer.IsEnabled) {
                return null;
            }
            foreach (var instance in actor.QuirkContainer.GetInstances()) {
                if (instance.Definition.Tags.Contains("quirk_curse")) {
                    return QuirkDescription.GetNameString(instance.Definition, actor, appendRareIcon: false);
                }
            }
            return null;
        }

        // The header button's own tooltip (the vitals bar) and the tooltip of the hero's
        // portrait on its inn (the travel rule, immobility, cancelling a trip).
        private IEnumerable<string> HeroDetailLines(KingdomMapActorHeaderButtonBhv button) {
            foreach (var line in TooltipReader.Lines(button.gameObject)) {
                yield return line;
            }
            var mapButton = MapActorButton(button.ActorGuid);
            if (mapButton != null) {
                foreach (var line in TooltipReader.Lines(mapButton.gameObject)) {
                    yield return line;
                }
            }
        }

        // The hero's portrait button on its map cell; the party has none.
        private static KingdomActorButtonBhv MapActorButton(uint guid) {
            var mgr = Manager();
            var cell = mgr.KingdomMap.TryGetCell(guid);
            if (cell == null) {
                return null;
            }
            var view = mgr.KingdomMapRoot[cell.Coordinates];
            if (view == null) {
                return null;
            }
            foreach (var button in view.gameObject.GetComponentsInChildren<KingdomActorButtonBhv>()) {
                if (button.ActorGuid == guid) {
                    return button;
                }
            }
            return null;
        }

        // ---- Tree ----

        private void Populate() {
            _cursorElement = new ActionElement(CursorLine, null, ActivateCell, CursorDetail);
            _root.Add(_cursorElement);

            var ui = UnityEngine.Object.FindObjectOfType<KingdomUiBhv>();
            var header = new Container(ContainerShape.VerticalList);
            header.Add(new ReadoutElement(() =>
                string.Format(GameLoc.TryGet("kingdom_day_label") ?? "{0}",
                    SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomManager.Day)));
            if (ui != null) {
                AddPassDayButton(header, ui);
                AddNamedButton(header, ui, "Raycast");
            }
            var escalation = UnityEngine.Object.FindObjectOfType<KingdomMapGangEscalationBhv>();
            if (escalation != null) {
                header.Add(new ReadoutElement(
                    () => EscalationLine(),
                    detail: () => TooltipReader.Lines(escalation.gameObject)));
            }
            var timeline = UnityEngine.Object.FindObjectOfType<KingdomMapTimelineBhv>();
            if (timeline != null) {
                header.Add(new ReadoutElement(
                    () => TimelineLine(timeline),
                    detail: () => TimelineDetail(timeline)));
            }
            _root.Add(header);

            var sidebar = new Container(ContainerShape.VerticalList);
            foreach (var siege in SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomSiegeManager.KingdomSiegeInstances) {
                var captured = siege;
                sidebar.Add(new ActionElement(
                    () => SpokenLine.Join(S.KingdomSiege, InnNameAt(captured.m_Coordinates),
                        string.Format(GameLoc.TryGet("kingdom_map_days_remaining") ?? "{0}", captured.Delay)),
                    S.RoleButton,
                    () => {
                        // The jump hands the cursor straight back: arrows move the grid and
                        // Tab reads the header next, with the landing already spoken.
                        JumpTo(captured.m_Coordinates);
                        _navigator.Focus(_cursorElement, announce: false);
                    }));
            }
            // The sidebar's cursed-regions counter, which the game shows once a region is
            // infected; its tooltip's own text.
            if (Manager().GetBiomeModifierTagCount("infection") > 0) {
                sidebar.Add(new ReadoutElement(() => CursedRegionsLine()));
            }
            if (!sidebar.IsEmptyContainer) {
                _root.Add(sidebar);
            }

            _heroes = null;
            var headerBhv = UnityEngine.Object.FindObjectOfType<KingdomMapActorHeaderBhv>();
            if (headerBhv != null) {
                var heroes = new Container(ContainerShape.HorizontalList);
                foreach (var button in headerBhv.GetComponentsInChildren<KingdomMapActorHeaderButtonBhv>(includeInactive: false)) {
                    var selectable = button.GetComponent<Selectable>();
                    if (selectable != null) {
                        heroes.Add(new HeroElement(this, button, selectable));
                    }
                }
                if (!heroes.IsEmptyContainer) {
                    _root.Add(heroes);
                    _heroes = heroes;
                }
            }

            // The footer's sheet and inventory buttons, captioned with the keys the game
            // advertises for them (C, I), which the captured keyboard routes to the buttons.
            if (ui != null) {
                var footer = new Container(ContainerShape.HorizontalList);
                AddNamedButton(footer, ui, "CharacterSheetBtn");
                AddNamedButton(footer, ui, "InventoryBtn");
                if (!footer.IsEmptyContainer) {
                    _root.Add(footer);
                }
            }
            _builtSignature = Signature();
        }

        // The pass-day button is a hold gesture with no onClick; its element runs the game's
        // own commit.
        private static void AddPassDayButton(Container container, KingdomUiBhv ui) {
            foreach (var button in ui.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.gameObject.name == "DayPassButton") {
                    container.Add(new KingdomPassDayElement(ui, button));
                    return;
                }
            }
        }

        private static void AddNamedButton(Container container, KingdomUiBhv ui, string name) {
            foreach (var button in ui.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.gameObject.name == name) {
                    container.Add(new SelectableElement(button));
                    return;
                }
            }
        }

        private static string EscalationLine() {
            int level = (int)Singleton<Assets.Code.Game.GameTypeMgr>.Instance.RunValues
                .GetValue(Assets.Code.Run.RunValueType.ESCALATION);
            return string.Format(GameLoc.TryGet("kingdom_map_escalation_tooltip_title") ?? "escalation {0}", level);
        }

        private static string CursedRegionsLine() {
            var mgr = Manager();
            return string.Format(GameLoc.TryGet("kingdom_map_cursed_regions_tooltip") ?? "{0} / {1}",
                mgr.GetBiomeModifierTagCount("infection"), mgr.GetBiomeCount());
        }

        private static string TimelineLine(KingdomMapTimelineBhv timeline) {
            foreach (var tip in timeline.GetComponentsInChildren<Assets.Code.UI.Tooltips.TextTooltipBhv>(includeInactive: false)) {
                if (tip.gameObject.name.StartsWith("LastDayNotch", StringComparison.Ordinal)) {
                    foreach (var line in TooltipReader.LinesOf(tip)) {
                        return line;
                    }
                }
            }
            return null;
        }

        // Only the marked days (escalation surges, completed quest steps, the last day) - the
        // plain per-day notches carry nothing but their number.
        private static IEnumerable<string> TimelineDetail(KingdomMapTimelineBhv timeline) {
            foreach (var tip in timeline.GetComponentsInChildren<Assets.Code.UI.Tooltips.TextTooltipBhv>(includeInactive: false)) {
                string owner = tip.gameObject.name;
                if (owner.StartsWith("EscalationStepNotch", StringComparison.Ordinal)
                    || owner.StartsWith("QuestStepNotch", StringComparison.Ordinal)
                    || owner.StartsWith("LastDayNotch", StringComparison.Ordinal)) {
                    foreach (var line in TooltipReader.LinesOf(tip)) {
                        yield return line;
                    }
                }
            }
        }

        private string InnNameAt(Vector2Int coords) {
            var view = Manager().KingdomMapRoot[coords] as KingdomMapCellBhv;
            var context = view == null ? null : view.GetComponentInChildren<DataContextBhv>(includeInactive: false);
            return context == null ? null : context.GetStringValue("cell_name");
        }

        private int Signature() {
            int signature = 17;
            var kingdom = SingletonMonoBehaviour<KingdomBhv>.Instance;
            foreach (var siege in kingdom.KingdomSiegeManager.KingdomSiegeInstances) {
                signature = signature * 31 + siege.m_Coordinates.x;
                signature = signature * 31 + siege.m_Coordinates.y;
            }
            signature = signature * 31 + (kingdom.KingdomMapManager.GetBiomeModifierTagCount("infection") > 0 ? 1 : 0);
            var headerBhv = UnityEngine.Object.FindObjectOfType<KingdomMapActorHeaderBhv>();
            if (headerBhv != null) {
                foreach (var button in headerBhv.GetComponentsInChildren<KingdomMapActorHeaderButtonBhv>(includeInactive: false)) {
                    signature = signature * 31 + button.GetInstanceID();
                }
            }
            return signature;
        }

        /// <summary>
        /// A hero in the map's header row. Enter goes to the screen's hero handling (the party
        /// jump or the game's hero-travel mode); the sheet key opens the hero's sheet the way
        /// the game's right-click does. The hero buffer carries the hero's vitals.
        /// </summary>
        private sealed class HeroElement : SelectableElement {
            private readonly KingdomMapScreen _screen;
            private readonly KingdomMapActorHeaderButtonBhv _button;

            public HeroElement(KingdomMapScreen screen, KingdomMapActorHeaderButtonBhv button, Selectable selectable)
                : base(selectable) {
                _screen = screen;
                _button = button;
            }

            public uint Guid => _button.ActorGuid;

            public override string Label => _screen.HeroHeaderLine(_button);

            public override IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, () => _screen.ActivateHero(this));
                yield return new ElementAction("inspect", _button.OpenCharacterSheet);
            }

            // The game's own submit press on the button: hero-travel mode for a stationed
            // hero, refused for the party, an immobile hero, or outside the player's turn.
            public void PressGame() => Submit();

            protected override IEnumerable<string> GetDetailLines() => _screen.HeroDetailLines(_button);

            public override IEnumerable<string> GetSideBufferLines(string bufferKey)
                => bufferKey == Core.Buffers.BufferKeys.Hero
                    ? HeroStatus.Lines(Guid) : base.GetSideBufferLines(bufferKey);
        }
    }
}
