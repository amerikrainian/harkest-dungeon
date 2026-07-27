using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Kingdom;
using Assets.Code.Kingdom.Events;
using Assets.Code.Kingdom.Presentation;
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
    /// days, one element per active siege (Enter jumps the cursor there), and the party and
    /// reserve hero rows (Enter on a reserve hero starts the game's hero-travel mode).
    /// Escape closes through CloseMap, which the game refuses mid day-turn.
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
        private int _builtSignature;

        public KingdomMapScreen(TraditionalNavigator navigator, Action<string, bool> speak, Action cursorMoved) {
            _navigator = navigator;
            _speak = speak;
            _cursorMoved = cursorMoved;
        }

        public override string Name => S.ScreenKingdomMap;

        public override object ResolveTarget() {
            if (!SingletonMonoBehaviour<KingdomBhv>.HasInstance() || !KingdomBhv.IsMapOpen()) {
                return null;
            }
            // Any pushed screen (a cell panel, the hero sheet) covers the map and reads instead.
            if (SingletonMonoBehaviour<ScreenStackBhv>.HasInstance()) {
                var top = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
                if (top != null && top.m_screenType == ScreenStackBhv.ScreenOrderType.SCREEN && top.m_screenObj != null) {
                    return null;
                }
            }
            return UnityEngine.Object.FindObjectOfType<KingdomPresentationBhv>();
        }

        public override Container BuildRoot(object target) {
            // The cursor survives a panel round-trip (the map stayed open behind it) and homes
            // to the stagecoach on a fresh open.
            if (!_sessionLive || !Manager().KingdomMap.IsValidCoordinate(_cursor)) {
                _cursor = Manager().CurrentStageCoachCoordinates;
            }
            _sessionLive = true;
            // A Panel root so Tab crosses cursor / header / sieges / heroes - the cursor
            // element owns the arrows while focused, so Tab is the only way off it.
            _root = new RootContainer(ContainerShape.Panel, back: () => {
                var presentation = (KingdomPresentationBhv)target;
                if (!presentation.CloseMap()) {
                    _speak(S.StatusUnavailable, true);
                }
            });
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
            if (_cursor == mgr.CurrentStageCoachCoordinates) {
                parts.Add(S.KingdomStagecoach);
            } else if (mgr.HasNextStageCoachCoordinates && _cursor == mgr.NextStageCoachCoordinates) {
                parts.Add(S.KingdomTravelScheduled);
            } else if (Reachable(mgr)) {
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

        private string PositionWords() =>
            S.KingdomCell(_cursor.y + 1, Manager().KingdomMap.NumberOfRows,
                          _cursor.x + 1, Manager().KingdomMap.NumberOfCols);

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
            EventKingdomActivateMapCell.Trigger(_cursor, wasSelectedByPointer: false);
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
                AddNamedButton(header, ui, "DayPassButton");
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

            var sieges = new Container(ContainerShape.VerticalList);
            foreach (var siege in SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomSiegeManager.KingdomSiegeInstances) {
                var captured = siege;
                sieges.Add(new ActionElement(
                    () => SpokenLine.Join(S.KingdomSiege, InnNameAt(captured.m_Coordinates),
                        string.Format(GameLoc.TryGet("kingdom_map_days_remaining") ?? "{0}", captured.Delay)),
                    S.RoleButton,
                    () => JumpTo(captured.m_Coordinates)));
            }
            if (!sieges.IsEmptyContainer) {
                _root.Add(sieges);
            }

            var headerBhv = UnityEngine.Object.FindObjectOfType<KingdomMapActorHeaderBhv>();
            if (headerBhv != null) {
                var heroes = new Container(ContainerShape.HorizontalList);
                foreach (var button in headerBhv.GetComponentsInChildren<KingdomMapActorHeaderButtonBhv>(includeInactive: false)) {
                    var captured = button;
                    var selectable = button.GetComponent<Selectable>();
                    if (selectable != null) {
                        heroes.Add(new SelectableElement(selectable, () => HeroHeaderLine(captured)));
                    }
                }
                if (!heroes.IsEmptyContainer) {
                    _root.Add(heroes);
                }
            }
            _builtSignature = Signature();
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

        private string HeroHeaderLine(KingdomMapActorHeaderButtonBhv button) {
            var actor = Actors.Get(button.ActorGuid);
            if (actor == null) {
                return UiText.FirstLabel(button.gameObject);
            }
            string travelling = Manager().DoesActorHaveTransfer(button.ActorGuid)
                ? S.KingdomTravelScheduled : null;
            return SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id), travelling);
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
            var headerBhv = UnityEngine.Object.FindObjectOfType<KingdomMapActorHeaderBhv>();
            if (headerBhv != null) {
                foreach (var button in headerBhv.GetComponentsInChildren<KingdomMapActorHeaderButtonBhv>(includeInactive: false)) {
                    signature = signature * 31 + button.GetInstanceID();
                }
            }
            return signature;
        }
    }
}
