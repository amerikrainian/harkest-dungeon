using System;
using Assets.Code.Game;
using Assets.Code.Inn;
using Assets.Code.UI.Items;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
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
    /// the station buttons (Travelogue, End Expedition, the shops when the inn has them;
    /// captions live in their tooltips), then the inventory panel: filter, slot count, and
    /// wallet readouts, the sort button (its press confirms "sorted by type" - the game's one
    /// sort), one element per carried item (title and stack, full tooltip in the buffer), and
    /// the free capacity as one collapsed line. A station opens through its own button; the
    /// opened sub-screen is read by its dedicated screen or the generic floor. Escape opens
    /// the pause menu.
    /// </summary>
    public sealed class InnScreen : GameScreen {
        private readonly Action<string, bool> _speak;
        private SubScreenCollectionBhv _collection;
        private InventoryUiBhv _inventory;
        private Container _root;
        private Container _heroes;
        private Container _buttons;
        private Container _items;
        private int _builtButtonsSignature;
        private int _builtItemsSignature;

        public InnScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name {
            get {
                // The instance's Name already holds the inn's localized title.
                var inn = Singleton<InnBhv>.Instance != null ? Singleton<InnBhv>.Instance.GetInnInstance() : null;
                return inn != null && !string.IsNullOrEmpty(inn.Name) ? inn.Name : S.ScreenInn;
            }
        }

        // Matches only while the stack shows nothing above the inn's own inventory panel - any
        // station sub-screen on top hands the surface to that screen's reader instead.
        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.INN || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _collection = null;
                _inventory = null;
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
                back: () => SingletonMonoBehaviour<CommonUiBhv>.Instance.TogglePauseMenu());

            var regions = FindChild(_collection.transform.parent, "RegionsToMountain");
            if (regions != null) {
                _root.Add(new ReadoutElement(() => regions == null ? null : UiText.AllText(regions.gameObject)));
            }

            _heroes = new Container(ContainerShape.HorizontalList, S.CrossroadsParty);
            _root.Add(_heroes);
            PopulateHeroes();

            _buttons = new Container(ContainerShape.VerticalList);
            _root.Add(_buttons);
            PopulateButtons();

            // The inventory's frame (filter, count, wallet, sort) lives on persistent widgets,
            // so these elements are stable across re-sorts; only the pooled item slots below
            // ever need a rebuild. That keeps focus on Sort alive through its own press.
            if (_inventory != null) {
                var filter = FindChild(_inventory.transform, "ActiveFilter");
                if (filter != null) {
                    _root.Add(new ReadoutElement(() => filter == null ? null : UiText.AllText(filter.gameObject)));
                }
                var count = FindChild(_inventory.transform, "SlotCountContainer");
                if (count != null) {
                    _root.Add(new ReadoutElement(() => {
                        string text = count == null ? null : UiText.AllText(count.gameObject);
                        return string.IsNullOrEmpty(text) ? null : S.InventorySlots(text);
                    }));
                }
                var currencies = FindChild(_inventory.transform, "Currencies");
                if (currencies != null) {
                    foreach (Transform row in currencies) {
                        var captured = row;
                        _root.Add(new ReadoutElement(() => CurrencyLine(captured)));
                    }
                }
                foreach (var selectable in _inventory.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                    if (selectable.GetComponent<InventoryItemBhv>() != null || !Include(selectable)) {
                        continue;
                    }
                    var button = selectable;
                    if (button.gameObject.name == "SortButton") {
                        _root.Add(new ActionElement(() => UiText.FirstLabel(button.gameObject), S.RoleButton, () => {
                            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current),
                                ExecuteEvents.submitHandler);
                            _speak(S.InventorySorted, false);
                        }));
                    } else {
                        _root.Add(new SelectableElement(button));
                    }
                }
            }

            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            PopulateItems();
            return _root;
        }

        public override bool OnUpdate(object target) {
            if (ButtonsSignature() != _builtButtonsSignature) {
                PopulateButtons();
            }
            if (_inventory != null && ItemsSignature() != _builtItemsSignature) {
                PopulateItems();
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

        // The station buttons are icon-only; their captions come from their tooltips, which
        // UiText.FirstLabel already resolves.
        private void PopulateButtons() {
            _buttons.Clear();
            if (_collection == null) {
                _builtButtonsSignature = 0;
                return;
            }
            foreach (var selectable in _collection.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (Include(selectable)) {
                    _buttons.Add(new SelectableElement(selectable));
                }
            }
            _builtButtonsSignature = ButtonsSignature();
        }

        // What the player carries - occupied slots only, with the free capacity collapsed to
        // one live line (bag position carries no meaning; the game's own sort reorders freely).
        private void PopulateItems() {
            _items.Clear();
            if (_inventory == null) {
                _builtItemsSignature = 0;
                return;
            }
            foreach (var slot in _inventory.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (slot.IsOccupied) {
                    var selectable = slot.GetComponent<Selectable>();
                    if (selectable != null) {
                        _items.Add(new InventoryItemElement(slot, selectable));
                    }
                }
            }
            var inventory = _inventory;
            _items.Add(new ReadoutElement(() => {
                int empty = CountEmpty(inventory);
                return empty > 0 ? S.InventoryEmptySlots(empty) : null;
            }));
            _builtItemsSignature = ItemsSignature();
        }

        // A wallet row ("Relics, 40"): the caption is the row's tooltip, the amount its label.
        private static string CurrencyLine(Transform row) {
            if (row == null || !row.gameObject.activeInHierarchy) {
                return null;
            }
            string caption = null;
            foreach (var line in TooltipReader.Lines(row.gameObject)) {
                caption = line;
                break;
            }
            if (caption == null) {
                return null;
            }
            return SpokenLine.Join(caption, UiText.AllText(row.gameObject));
        }

        private static bool Include(Selectable selectable) {
            if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                return false;
            }
            return UiText.HasAnyTextSource(selectable.gameObject);
        }

        // Identity signatures, not counts: the game's pooled lists recycle and respawn their
        // widgets on a re-sort or a station rebuild, leaving the same number of NEW instances -
        // a count check reads equal while every held reference is dead.
        private int ButtonsSignature() {
            int signature = 17;
            if (_collection == null) {
                return 0;
            }
            foreach (var selectable in _collection.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (Include(selectable)) {
                    signature = signature * 31 + selectable.GetInstanceID();
                }
            }
            return signature;
        }

        private int ItemsSignature() {
            int signature = 17;
            if (_inventory == null) {
                return 0;
            }
            foreach (var slot in _inventory.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (slot.IsOccupied) {
                    signature = signature * 31 + slot.GetInstanceID();
                }
            }
            return signature;
        }

        private static int CountEmpty(InventoryUiBhv inventory) {
            if (inventory == null) {
                return 0;
            }
            int count = 0;
            foreach (var slot in inventory.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (!slot.IsOccupied) {
                    count++;
                }
            }
            return count;
        }

        private static Transform FindChild(Transform root, string name) {
            if (root == null) {
                return null;
            }
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: false)) {
                if (child.name == name) {
                    return child;
                }
            }
            return null;
        }
    }
}
