using System;
using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Run;
using Assets.Code.UI.Banter;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using DD2A11y.Input;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// Free driving (the DRIVING mode floor, below the fork menu and the road map). The first
    /// Tab stop is the driving area, where every key stays the game's - arrows and WASD drive,
    /// M, I, C, Z, G, Alt, Ctrl, and Escape all work as shipped - and the mod claims only Tab
    /// (the game's second minimap key) for panel traversal. The other stops read the rest of
    /// the HUD: the status readouts (distance, region, the flame with its Alt panel's state
    /// and effects in the buffer, armor, wheels, the Loathing meter), the hero ribbons (name
    /// with HP and stress; Enter is
    /// the ribbon's own inspect, Space grabs for a marching-order move that shifts slots the
    /// way the game's drag does), the goals panel while the game shows it (G toggles it), and
    /// the HUD buttons. Off the driving area the arrows, Space, Enter, and bare Ctrl rest so
    /// our list navigation cannot steer the coach; WASD and the letter hotkeys stay live
    /// everywhere. Escape stays the game's pause everywhere.
    /// </summary>
    public sealed class DrivingScreen : GameScreen {
        private static readonly AccessTools.FieldRef<GameUIBhv, GameObject> MutatorContainerField =
            AccessTools.FieldRefAccess<GameUIBhv, GameObject>("m_biomePanelMutatorContainer");
        private static readonly AccessTools.FieldRef<GameUIBhv, GameObject> GoalContainerField =
            AccessTools.FieldRefAccess<GameUIBhv, GameObject>("m_biomePanelGoalContainer");
        private static readonly AccessTools.FieldRef<HeroRibbonContainerBhv, int> SlotIndexField =
            AccessTools.FieldRefAccess<HeroRibbonContainerBhv, int>("m_currentSlotPositionIndex");
        private static readonly AccessTools.FieldRef<GameUIBhv, UnityEngine.Playables.PlayableDirector> DirectorField =
            AccessTools.FieldRefAccess<GameUIBhv, UnityEngine.Playables.PlayableDirector>("m_biomePanelDirector");
        private static readonly AccessTools.FieldRef<GameUIBhv, List<GameObject>> HeroObjectivesField =
            AccessTools.FieldRefAccess<GameUIBhv, List<GameObject>>("m_heroObjectiveObjects");
        private static readonly AccessTools.FieldRef<BiomePanelHeroGoalBhv, TextMeshProUGUI> GoalTextField =
            AccessTools.FieldRefAccess<BiomePanelHeroGoalBhv, TextMeshProUGUI>("m_goalText");
        private static readonly AccessTools.FieldRef<GameUIBhv, Assets.Code.UI.StageCoachTorchUiBhv> TorchField =
            AccessTools.FieldRefAccess<GameUIBhv, Assets.Code.UI.StageCoachTorchUiBhv>("m_stageCoachTorch");
        private static readonly AccessTools.FieldRef<Assets.Code.UI.StageCoachTorchUiBhv, Assets.Code.Data.DataContextBhv> TorchPanelField =
            AccessTools.FieldRefAccess<Assets.Code.UI.StageCoachTorchUiBhv, Assets.Code.Data.DataContextBhv>("m_tooltipDataContext");

        private readonly Action<string, bool> _speak;
        private readonly TraditionalNavigator _navigator;
        // The panel-cycling keys (Tab by default, the game's second minimap key) are the mod's
        // everywhere, so their game bindings rest for the whole stand; M keeps the map. The
        // claim follows the live bindings, so a rebind hands the freed key back to the game.
        private readonly DrivingKeySuppressor _tabKey;
        // Off the driving area the list keys walk our elements, so their game bindings
        // (steering, Interact, the Ctrl glossary hold) rest.
        private readonly DrivingKeySuppressor _listKeys;
        private readonly Dictionary<HeroRibbonBhv, HeroRibbonElement> _heroElements =
            new Dictionary<HeroRibbonBhv, HeroRibbonElement>();
        private readonly Dictionary<UnityEngine.Object, UIElement> _goalRows =
            new Dictionary<UnityEngine.Object, UIElement>();

        private GameUIBhv _hud;
        private HeroRibbonContainerBhv _ribbonContainer;
        private Container _root;
        private Container _status;
        private Container _heroes;
        private Container _goals;
        private Container _buttons;
        private UIElement _drivingArea;
        private HeroRibbonBhv _held;
        private int _builtSignature;
        private bool _goalsWereOpen;

        public DrivingScreen(Action<string, bool> speak, TraditionalNavigator navigator,
                             Core.Input.InputManager input) {
            _speak = speak;
            _navigator = navigator;
            _tabKey = new DrivingKeySuppressor(
                () => DrivingKeySuppressor.ClaimFor(input, UiActions.Next, UiActions.Prev),
                navigationEvents: false);
            _listKeys = new DrivingKeySuppressor(
                () => DrivingKeySuppressor.ClaimFor(input,
                    UiActions.Up, UiActions.Down, UiActions.Left, UiActions.Right,
                    UiActions.Home, UiActions.End, UiActions.Activate,
                    "ui.grab", "ui.place.one", "ui.discard",
                    "buffer.next", "buffer.prev", "buffer.line.next", "buffer.line.prev"),
                navigationEvents: true);
        }

        public override string Name => S.ScreenDriving;

        public override bool CapturesKeyboard => false;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.DRIVING
                || Singleton<GameModeMgr>.Instance.IsChangingState()
                || !SingletonMonoBehaviour<GameUIBhv>.HasInstance()) {
                _hud = null;
                return null;
            }
            _hud = SingletonMonoBehaviour<GameUIBhv>.Instance;
            return _hud;
        }

        public override Container BuildRoot(object target) {
            var hud = (GameUIBhv)target;
            _held = null;
            _ribbonContainer = UnityEngine.Object.FindObjectOfType<HeroRibbonContainerBhv>();
            _tabKey.Reassert();

            // A Panel root so Tab crosses driving area / status / heroes / goals / buttons,
            // wrapping - the driving area is always one or two presses away. No back action:
            // Escape stays the game's pause menu.
            _root = new RootContainer(ContainerShape.Panel);
            _root.WrapTabStops = true;

            _drivingArea = new ReadoutElement(AreaLine);
            _root.Add(_drivingArea);

            _status = new Container(ContainerShape.VerticalList);
            AddStatus(hud);
            _root.Add(_status);

            _heroes = new Container(ContainerShape.HorizontalList, S.CrossroadsParty);
            _heroElements.Clear();
            _goalRows.Clear();
            PopulateHeroes();
            _root.Add(_heroes);

            var goalButton = FindChild(hud.transform, "BiomeGoalButton");
            _goals = new Container(ContainerShape.VerticalList,
                goalButton == null ? null : UiText.FirstLabel(goalButton.gameObject));
            PopulateGoals(hud);
            _root.Add(_goals);

            _buttons = new Container(ContainerShape.VerticalList);
            AddButton(hud, "MapBtn");
            AddButton(hud, "BiomeGoalButton");
            AddButton(hud, "InventoryBtn");
            AddButton(hud, "StageCoachBtn");
            AddButton(hud, "LastChanceTrophyContainer");
            _root.Add(_buttons);

            _builtSignature = Signature(hud);
            // Entering with the panel already open is not a summon; no focus jump then.
            _goalsWereOpen = hud.IsBiomePanelActive;
            return _root;
        }

        public override bool OnUpdate(object target) {
            var hud = (GameUIBhv)target;
            _tabKey.Reassert();
            if (_navigator.Current == _drivingArea) {
                _listKeys.Restore(immediate: false);
            } else {
                _listKeys.Reassert();
            }
            if (Signature(hud) != _builtSignature) {
                PopulateHeroes();
                PopulateGoals(hud);
                _builtSignature = Signature(hud);
            }
            // The goals panel is player-summoned (G or its button), so on the open's edge -
            // once, when its rows become focusable a beat after the toggle - focus jumps to
            // the first row and the router reads the panel out; the close re-homes to the
            // driving area through the orphan path. The edge keys to the game's own open
            // flag: row active-states flicker through the open timeline, and a content edge
            // would re-fire on every flicker.
            if (hud.IsBiomePanelActive) {
                if (!_goalsWereOpen) {
                    var first = _goals.FirstFocusable();
                    if (first != null) {
                        _goalsWereOpen = true;
                        _navigator.Focus(first, announce: false);
                        _listKeys.Reassert();
                        return true;
                    }
                }
            } else {
                _goalsWereOpen = false;
            }
            return false;
        }

        public override void OnLeave() {
            _tabKey.Restore();
            _listKeys.Restore();
            _held = null;
        }

        // On the driving area the arrows belong to the game (steering, speed); consuming them
        // keeps the mod's own focus parked there.
        public override bool HandleAction(string actionKey) {
            if (_navigator.Current != _drivingArea) {
                return false;
            }
            switch (actionKey) {
                case UiActions.Up:
                case UiActions.Down:
                case UiActions.Left:
                case UiActions.Right:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The grab key: pick up the focused hero, then place on another hero's slot.
        /// The in-between ribbons shift exactly as the game's own drag shifts them, each move
        /// committing through the ribbon's SetSlot (the actor's team position); the landing
        /// speaks the resulting marching order.</summary>
        public void ToggleGrab(UIElement current) {
            var element = current as HeroRibbonElement;
            if (element == null) {
                return;
            }
            var ribbon = element.Ribbon;
            if (_held == null) {
                if (ribbon.IsLocked()) {
                    _speak(S.StatusUnavailable, true);
                    return;
                }
                _held = ribbon;
                _speak(S.Grabbed(element.Label), true);
                return;
            }
            if (_held == ribbon) {
                _held = null;
                _speak(S.GrabCancelled, true);
                return;
            }
            if (ribbon.IsLocked()) {
                _speak(S.StatusUnavailable, true);
                return;
            }
            PlaceAt(_held, ribbon.GetSlot());
            _held = null;
            _speak(OrderLine(), true);
        }

        // ---- Reorder (the game's own drag, run logically) ----

        // The gamepad reorder's own calls: the hover index is where the drag consumes its
        // target slot, OnHeroRibbonDrag runs the game's slot shifts (locks included), and the
        // release settles positions and clears the drag state.
        private void PlaceAt(HeroRibbonBhv dragged, int target) {
            SlotIndexField(_ribbonContainer) = target;
            _ribbonContainer.OnHeroRibbonDrag(dragged, useMousePos: false);
            _ribbonContainer.OnHeroRibbonRelease(dragged);
        }

        // The resulting order, hero names left to right - the strip as drawn.
        private string OrderLine() {
            var names = new List<string>();
            foreach (var ribbon in ActiveRibbons()) {
                var actor = Actors.Get(ribbon.ActorGuid);
                if (actor != null) {
                    names.Add(Actors.Name(actor));
                }
            }
            return Core.Text.SpokenLine.Join(names.ToArray());
        }

        private List<HeroRibbonBhv> ActiveRibbons() {
            var ribbons = new List<HeroRibbonBhv>();
            if (_ribbonContainer == null) {
                return ribbons;
            }
            foreach (var ribbon in _ribbonContainer.GetComponentsInChildren<HeroRibbonBhv>(includeInactive: false)) {
                if (ribbon.ActorGuid != 0) {
                    ribbons.Add(ribbon);
                }
            }
            // Descending slots = left to right as the game draws the strip (slot 0, the
            // front line, sits rightmost - measured live 2026-08-08), the same direction
            // the combat battlefield row and the crossroads slots walk the party.
            ribbons.Sort((a, b) => b.GetSlot().CompareTo(a.GetSlot()));
            return ribbons;
        }

        // ---- Tree ----

        // The biome name the minimap widget shows; the authored fallback covers its absence.
        private string AreaLine() {
            var label = FindChild(_hud == null ? null : _hud.transform, "ActiveBiomeLabel");
            var tmp = label == null ? null : label.GetComponent<TMP_Text>();
            string biome = tmp == null ? null : tmp.text;
            return string.IsNullOrWhiteSpace(biome) ? S.DrivingRoad : biome;
        }

        private void AddStatus(GameUIBhv hud) {
            var transform = hud.transform;
            AddHudLabel(transform, "DistanceLabel");
            AddHudLabel(transform, "Region");
            // The flame readout. Holding Alt only plays the visual intro of the torch panel;
            // the panel's content - the state name and the per-side effects the game stamps
            // into its DataContext on every torch change - reads here as the buffer.
            var torch = FindChild(transform, "StageCoachTorch");
            var torchUi = TorchField(hud);
            if (torch != null && torchUi != null) {
                var counterHost = FindChild(torch, "UI");
                var counter = counterHost == null ? null : counterHost.GetComponentInChildren<TMP_Text>(false);
                _status.Add(new ReadoutElement(
                    () => counter == null ? null : S.DrivingFlame(counter.text),
                    detail: () => FlameLines(torchUi)));
            }
            // The same game strings the stagecoach sheet composes, over the live run values.
            var armor = FindChild(transform, "ArmorContainer");
            _status.Add(new ReadoutElement(
                () => RunStatus.CoachStat("stage_coach_sheet_armor_stat_label",
                    RunValueType.STAGE_COACH_ARMOR, RunStatType.STAGE_COACH_ARMOR_MAX_VALUE),
                detail: () => TooltipReader.Lines(armor == null ? null : armor.gameObject)));
            var wheels = FindChild(transform, "WheelContainer");
            _status.Add(new ReadoutElement(
                () => RunStatus.CoachStat("stage_coach_sheet_wheel_stat_label",
                    RunValueType.STAGE_COACH_WHEELS, RunStatType.STAGE_COACH_WHEELS_MAX_VALUE),
                detail: () => TooltipReader.Lines(wheels == null ? null : wheels.gameObject)));
            // The Loathing meter names itself through its own tooltip.
            var doom = FindChild(transform, "DoomMeter");
            if (doom != null) {
                var doomScope = doom.gameObject;
                _status.Add(new ReadoutElement(
                    () => FirstLine(TooltipReader.Lines(doomScope)),
                    detail: () => TooltipReader.Lines(doomScope)));
            }
        }

        // The Alt panel's lines: the flame state name, then each side's effects under the
        // game's own caption ("Heroes, +6% death RES"), one effect per line.
        private static IEnumerable<string> FlameLines(Assets.Code.UI.StageCoachTorchUiBhv torchUi) {
            var context = TorchPanelField(torchUi);
            if (context == null) {
                yield break;
            }
            string title = context.GetStringValue("torch_title");
            if (!string.IsNullOrWhiteSpace(title)) {
                yield return title;
            }
            foreach (var line in FlameSideLines(context, "party_heroes_label", "torch_effects_heroes")) {
                yield return line;
            }
            foreach (var line in FlameSideLines(context, "party_enemies_label", "torch_effects_enemies")) {
                yield return line;
            }
        }

        private static IEnumerable<string> FlameSideLines(
            Assets.Code.Data.DataContextBhv context, string captionKey, string valueKey) {
            string effects = context.GetStringValue(valueKey);
            if (string.IsNullOrWhiteSpace(effects)) {
                yield break;
            }
            string caption = GameLoc.TryGet(captionKey);
            foreach (var line in effects.Split('\n')) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    yield return Core.Text.SpokenLine.Join(caption, line.Trim());
                    caption = null; // the caption heads only its side's first line
                }
            }
        }

        private void AddHudLabel(Transform hud, string name) {
            var child = FindChild(hud, name);
            var tmp = child == null ? null : child.GetComponent<TMP_Text>();
            if (tmp != null) {
                _status.Add(new ReadoutElement(
                    () => tmp == null || !tmp.gameObject.activeInHierarchy ? null : tmp.text));
            }
        }

        private void PopulateHeroes() {
            _heroes.Clear();
            foreach (var ribbon in ActiveRibbons()) {
                if (!_heroElements.TryGetValue(ribbon, out var element)) {
                    element = new HeroRibbonElement(ribbon);
                    _heroElements[ribbon] = element;
                }
                _heroes.Add(element);
            }
        }

        // The panel's rows keep their objects but flicker their active states through the
        // open timeline, so the swept set includes inactive rows (a stable tree - no rebuild
        // churn, focus survives the open), every element is reused per row, and each label
        // answers null while its row is hidden so navigation skips it live.
        private void PopulateGoals(GameUIBhv hud) {
            _goals.Clear();
            if (!hud.IsBiomePanelActive) {
                return;
            }
            // The biome's mutator and goal sections, active only when the biome has them.
            AddGoalRows(MutatorContainerField(hud));
            AddGoalRows(GoalContainerField(hud));
            var director = DirectorField(hud);
            if (director == null) {
                return;
            }
            // Hero goal rows identify their hero by portrait only; the name comes through the
            // same row-to-party mapping the game's own populate writes. Completion shows in
            // the goal's own progress count; the reward is the row's tooltip.
            foreach (var row in director.GetComponentsInChildren<BiomePanelHeroGoalBhv>(includeInactive: true)) {
                if (!_goalRows.TryGetValue(row, out var element)) {
                    var captured = row;
                    element = new ReadoutElement(
                        () => {
                            if (!captured.gameObject.activeInHierarchy) {
                                return null;
                            }
                            var goalText = GoalTextField(captured);
                            return Core.Text.SpokenLine.Join(
                                Actors.Name(HeroForRow(hud, captured.gameObject)),
                                goalText == null ? null : goalText.text);
                        },
                        detail: () => TooltipReader.Lines(captured.gameObject));
                    _goalRows[row] = element;
                }
                _goals.Add(element);
            }
        }

        private static Assets.Code.Actor.ActorInstance HeroForRow(GameUIBhv hud, GameObject row) {
            var rows = HeroObjectivesField(hud);
            var party = Singleton<GameTypeMgr>.Instance.RosterManager.GetPartyActors();
            for (int i = 0; i < rows.Count && i < party.Count; i++) {
                if (rows[i] == row) {
                    return party[i];
                }
            }
            return null;
        }

        private void AddGoalRows(GameObject container) {
            if (container == null) {
                return;
            }
            foreach (Transform row in container.transform) {
                if (!_goalRows.TryGetValue(row, out var element)) {
                    var scope = row.gameObject;
                    element = new ReadoutElement(
                        () => scope.activeInHierarchy ? UiText.AllText(scope) : null,
                        detail: () => TooltipReader.Lines(scope));
                    _goalRows[row] = element;
                }
                _goals.Add(element);
            }
        }

        private void AddButton(GameUIBhv hud, string name) {
            // Inactive included: the last-chance trophy button exists from the start and only
            // activates near the biome's end; CanFocus follows its live state.
            var child = FindChild(hud.transform, name, includeInactive: true);
            var button = child == null ? null : child.GetComponent<Button>();
            if (button != null) {
                _buttons.Add(new SelectableElement(button));
            }
        }

        private int Signature(GameUIBhv hud) {
            int signature = 17;
            foreach (var ribbon in ActiveRibbons()) {
                signature = signature * 31 + ribbon.GetInstanceID();
                signature = signature * 31 + ribbon.GetSlot();
                signature = signature * 31 + (int)ribbon.ActorGuid;
            }
            signature = signature * 31 + (hud.IsBiomePanelActive ? 1 : 0);
            if (hud.IsBiomePanelActive) {
                signature = GoalSignature(MutatorContainerField(hud), signature);
                signature = GoalSignature(GoalContainerField(hud), signature);
                var director = DirectorField(hud);
                if (director != null) {
                    foreach (var row in director.GetComponentsInChildren<BiomePanelHeroGoalBhv>(includeInactive: true)) {
                        signature = signature * 31 + row.GetInstanceID();
                    }
                }
            }
            return signature;
        }

        // Over ALL children: active states flicker through the panel's open timeline, and a
        // signature that follows them would churn rebuilds every flicker.
        private static int GoalSignature(GameObject container, int signature) {
            if (container == null) {
                return signature;
            }
            foreach (Transform row in container.transform) {
                signature = signature * 31 + row.GetInstanceID();
            }
            return signature;
        }

        private static string FirstLine(IEnumerable<string> lines) {
            foreach (var line in lines) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    return line;
                }
            }
            return null;
        }

        private static Transform FindChild(Transform root, string name, bool includeInactive = false) {
            if (root == null) {
                return null;
            }
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive)) {
                if (child.name == name) {
                    return child;
                }
            }
            return null;
        }
    }
}
