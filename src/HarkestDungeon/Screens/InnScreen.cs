using System;
using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Inn;
using Assets.Code.UI;
using Assets.Code.UI.Inn;
using Assets.Code.UI.Items;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using HarmonyLib;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn hub (the INN game mode with nothing but its own inventory panel on the stack),
    /// named by the inn's own title. Layout, top to bottom: the regions-to-mountain readout,
    /// the hero rest strip (a horizontal row - name, HP, stress, status tooltip in the buffer),
    /// the stationed-heroes row (Kingdoms: the portrait strip by the inn title, each reading
    /// its class; Enter opens that hero's sheet the way the game's right-click does),
    /// the station buttons (Travelogue, End Expedition, the shops when the inn has them;
    /// captions live in their tooltips), then the inventory panel: the filter as a tab
    /// (Left/Right apply the game's own tab buttons), slot count and wallet readouts, the sort
    /// button (its press confirms "sorted by type" - the game's one sort), one element per
    /// carried item (title and stack, full tooltip in the buffer; Shift+Enter discards - or
    /// sells one, with a seller open - where the game's shift-click would), and the free
    /// capacity as one collapsed line. Space grabs the focused stack and places it on the
    /// next target (another stack to swap or combine, the capacity line for a free slot);
    /// while a grab is held, Shift+Space places a single item off it, repeatable until the
    /// stack runs out. A station opens through its own button; the opened sub-screen is read
    /// by its dedicated screen or the generic floor. Escape drops an armed grab first, else
    /// opens the pause menu.
    /// </summary>
    public sealed class InnScreen : GameScreen {
        private static readonly AccessTools.FieldRef<InnStationedActorBhv, uint> StationedGuidField =
            AccessTools.FieldRefAccess<InnStationedActorBhv, uint>("m_actorGuid");

        private readonly Action<string, bool> _speak;
        private readonly InventoryPanel _panel;
        private SubScreenCollectionBhv _collection;
        private InventoryUiBhv _inventory;
        private Container _root;
        private Container _heroes;
        private Container _stationed;
        private Container _buttons;
        private int _builtButtonsSignature;
        private int _builtStationedSignature;

        public InnScreen(Action<string, bool> speak, TraditionalNavigator navigator) {
            _speak = speak;
            _panel = new InventoryPanel(speak, navigator);
        }

        /// <summary>The grab key (Space / Shift+Space), routed here while this screen stands.</summary>
        public void ToggleGrab(Core.Nav.UIElement current, bool takeOne) => _panel.ToggleGrab(current, takeOne);

        public override string Name {
            get {
                // The instance's Name already holds the inn's localized title.
                var inn = Singleton<InnBhv>.Instance != null ? Singleton<InnBhv>.Instance.GetInnInstance() : null;
                return inn != null && !string.IsNullOrEmpty(inn.Name) ? inn.Name : S.ScreenInn;
            }
        }

        // Matches only while the stack shows nothing above the inn's own inventory panel - any
        // station sub-screen on top hands the surface to that screen's reader instead. The
        // veil gate keeps the hub from resolving early against a hidden stack and swapping
        // to the inventory target (a re-announce) when the reveal finishes.
        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.INN || StackTop.Veiled) {
                _collection = null;
                _inventory = null;
                Game.InnEvents.Clear();
                return null;
            }
            var top = StackTop.Object();
            _inventory = top == null ? null : top.GetComponent<InventoryUiBhv>();
            if (top != null && _inventory == null) {
                return null;
            }
            if (_collection == null) {
                _collection = UnityEngine.Object.FindObjectOfType<SubScreenCollectionBhv>();
            }
            if (_collection == null || !_collection.gameObject.activeInHierarchy) {
                return null;
            }
            return _inventory != null ? (object)_inventory : _collection;
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => {
                    if (_panel.GrabArmed) {
                        _panel.CancelGrab();
                    } else {
                        SingletonMonoBehaviour<CommonUiBhv>.Instance.TogglePauseMenu();
                    }
                });

            var regions = FindChild(_collection.transform.parent, "RegionsToMountain");
            if (regions != null) {
                _root.Add(new ReadoutElement(() => regions == null ? null : UiText.AllText(regions.gameObject)));
            }

            _heroes = new Container(ContainerShape.HorizontalList, S.CrossroadsParty);
            _root.Add(_heroes);
            PopulateHeroes();

            _stationed = new Container(ContainerShape.HorizontalList, S.InnStationedHeroes);
            _root.Add(_stationed);
            PopulateStationed();

            _buttons = new Container(ContainerShape.VerticalList);
            _root.Add(_buttons);
            PopulateButtons();

            // The inventory panel itself is the shared reader (frame first, then the pooled
            // item slots), the same one the standalone inventory screen shows alone.
            if (_inventory != null) {
                _panel.BuildInto(_root, _inventory);
            }
            return _root;
        }

        public override bool OnUpdate(object target) {
            // The inn's transient text (rest-item barks and refusals, affinity changes):
            // announced queued so a burst of reactions stacks in order, held until the entry
            // announcement is out like the combat log.
            if (EntryAnnounced) {
                var events = Game.InnEvents.Drain();
                if (events != null) {
                    foreach (var line in events) {
                        _speak(line, false);
                    }
                }
            }
            if (ButtonsSignature() != _builtButtonsSignature) {
                PopulateButtons();
            }
            if (StationedSignature() != _builtStationedSignature) {
                PopulateStationed();
            }
            if (_inventory != null) {
                _panel.Update();
            }
            return false;
        }

        // The rest strip: every hero slot, in the game's own left-to-right order. Slots without
        // a hero hide themselves through the element.
        private void PopulateHeroes() {
            _heroes.Clear();
            var slots = UnityEngine.Object.FindObjectsOfType<RestItemSlotBhv>();
            Array.Sort(slots, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            foreach (var slot in slots) {
                var selectable = slot.GetComponent<Selectable>();
                if (selectable != null) {
                    _heroes.Add(new RestHeroElement(slot, selectable));
                }
            }
        }

        // The stationed-hero portraits by the inn title (Kingdoms; the pool is empty
        // elsewhere): click-only widgets the selectable sweep misses. The game recycles the
        // pool on day changes, so an identity signature guards the rebuild.
        private void PopulateStationed() {
            _stationed.Clear();
            foreach (var widget in StationedActors()) {
                _stationed.Add(new StationedHeroElement(widget));
            }
            _builtStationedSignature = StationedSignature();
        }

        private static InnStationedActorBhv[] StationedActors() {
            var widgets = UnityEngine.Object.FindObjectsOfType<InnStationedActorBhv>();
            Array.Sort(widgets, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return widgets;
        }

        private int StationedSignature() {
            int signature = 17;
            foreach (var widget in StationedActors()) {
                signature = signature * 31 + widget.GetInstanceID();
            }
            return signature;
        }

        private static void OpenStationedSheet(InnStationedActorBhv widget) {
            uint guid = StationedGuidField(widget);
            if (guid != 0) {
                SingletonMonoBehaviour<CommonUiBhv>.Instance.ToggleCharacterSheet(
                    CharacterSheetUiBhv.Tab.Skills, guid, isSkillsEditable: true, isInventoryEditable: true);
            }
        }

        // The station buttons are icon-only; their captions come from their tooltips, which
        // UiText.FirstLabel already resolves.
        private void PopulateButtons() {
            _buttons.Clear();
            if (_collection == null) {
                _builtButtonsSignature = 0;
                return;
            }
            foreach (var selectable in _collection.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (InventoryPanel.Include(selectable)) {
                    _buttons.Add(new SelectableElement(selectable));
                }
            }
            _builtButtonsSignature = ButtonsSignature();
        }

        // An identity signature, not a count: the game's pooled lists recycle and respawn their
        // widgets on a station rebuild, leaving the same number of NEW instances - a count
        // check reads equal while every held reference is dead.
        private int ButtonsSignature() {
            int signature = 17;
            if (_collection == null) {
                return 0;
            }
            foreach (var selectable in _collection.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (InventoryPanel.Include(selectable)) {
                    signature = signature * 31 + selectable.GetInstanceID();
                }
            }
            return signature;
        }

        private static Transform FindChild(Transform root, string name) => InventoryPanel.FindChild(root, name);

        /// <summary>
        /// A stationed hero's portrait: name and class from the hero behind it (the widget's
        /// own tooltip names only the class - the portrait is what tells a sighted player who),
        /// the hero buffer for their vitals, and Enter or the sheet key opening their sheet
        /// through the same game method the widget's own right-click drives.
        /// </summary>
        private sealed class StationedHeroElement : UIElement {
            private readonly InnStationedActorBhv _widget;

            public StationedHeroElement(InnStationedActorBhv widget) {
                _widget = widget;
            }

            private uint Guid => StationedGuidField(_widget);

            public override bool CanFocus => _widget != null && _widget.gameObject.activeInHierarchy;

            public override string Label {
                get {
                    var actor = Guid == 0 ? null : Actors.Get(Guid);
                    return actor == null
                        ? UiText.FirstLabel(_widget.gameObject)
                        : SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id));
                }
            }

            public override string Role => S.RoleButton;

            public override IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, () => OpenStationedSheet(_widget));
                yield return new ElementAction("inspect", () => OpenStationedSheet(_widget));
            }

            public override IEnumerable<string> GetSideBufferLines(string bufferKey)
                => bufferKey == Core.Buffers.BufferKeys.Hero && Guid != 0
                    ? HeroStatus.Lines(Guid) : base.GetSideBufferLines(bufferKey);
        }
    }
}
