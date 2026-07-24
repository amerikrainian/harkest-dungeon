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
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn hub (the INN game mode with nothing but its own inventory panel on the stack),
    /// named by the inn's own title. Layout, top to bottom: the regions-to-mountain readout,
    /// the hero rest strip (a horizontal row - name, HP, stress, status tooltip in the buffer),
    /// the station buttons (Travelogue, End Expedition, the shops when the inn has them;
    /// captions live in their tooltips), then the inventory panel: filter, slot count, and
    /// wallet readouts, the sort button, one element per carried item (title and stack, full
    /// tooltip in the buffer), and the free capacity as one collapsed line. A station opens
    /// through its own button; the opened sub-screen is read by its dedicated screen or the
    /// generic floor. Escape opens the pause menu.
    /// </summary>
    public sealed class InnScreen : GameScreen {
        private SubScreenCollectionBhv _collection;
        private InventoryUiBhv _inventory;
        private Container _root;
        private Container _heroes;
        private Container _buttons;
        private Container _items;
        private int _builtButtons;
        private int _builtItems;

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
                _collection = Object.FindObjectOfType<SubScreenCollectionBhv>();
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

            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            PopulateInventory();
            return _root;
        }

        public override bool OnUpdate(object target) {
            if (CountSelectables(_collection) != _builtButtons) {
                PopulateButtons();
            }
            if (_inventory != null && CountOccupied(_inventory) != _builtItems) {
                PopulateInventory();
            }
            return false;
        }

        // The rest strip: every hero slot, in the game's own left-to-right order. Slots without
        // a hero hide themselves through the element.
        private void PopulateHeroes() {
            _heroes.Clear();
            var slots = Object.FindObjectsOfType<RestItemSlotBhv>();
            System.Array.Sort(slots, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
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
            _builtButtons = 0;
            if (_collection == null) {
                return;
            }
            foreach (var selectable in _collection.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (Include(selectable)) {
                    _buttons.Add(new SelectableElement(selectable));
                    _builtButtons++;
                }
            }
        }

        // The inventory panel: the filter, slot count, and wallet as readouts, then the sort
        // button, then what the player carries - occupied slots only, with the free capacity
        // collapsed to one line (bag position carries no meaning; the game's own sort reorders
        // freely).
        private void PopulateInventory() {
            _items.Clear();
            _builtItems = 0;
            if (_inventory == null) {
                return;
            }
            var filter = FindChild(_inventory.transform, "ActiveFilter");
            if (filter != null) {
                _items.Add(new ReadoutElement(() => filter == null ? null : UiText.AllText(filter.gameObject)));
            }
            var count = FindChild(_inventory.transform, "SlotCountContainer");
            if (count != null) {
                _items.Add(new ReadoutElement(() => {
                    string text = count == null ? null : UiText.AllText(count.gameObject);
                    return string.IsNullOrEmpty(text) ? null : S.InventorySlots(text);
                }));
            }
            var currencies = FindChild(_inventory.transform, "Currencies");
            if (currencies != null) {
                foreach (Transform row in currencies) {
                    var captured = row;
                    _items.Add(new ReadoutElement(() => CurrencyLine(captured)));
                }
            }
            foreach (var selectable in _inventory.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable.GetComponent<InventoryItemBhv>() == null && Include(selectable)) {
                    _items.Add(new SelectableElement(selectable));
                }
            }
            foreach (var slot in _inventory.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (slot.IsOccupied) {
                    var selectable = slot.GetComponent<Selectable>();
                    if (selectable != null) {
                        _items.Add(new InventoryItemElement(slot, selectable));
                        _builtItems++;
                    }
                }
            }
            var inventory = _inventory;
            _items.Add(new ReadoutElement(() => {
                int empty = CountEmpty(inventory);
                return empty > 0 ? S.InventoryEmptySlots(empty) : null;
            }));
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

        private static int CountSelectables(Component scope) {
            if (scope == null) {
                return 0;
            }
            int count = 0;
            foreach (var selectable in scope.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (Include(selectable)) {
                    count++;
                }
            }
            return count;
        }

        private static int CountOccupied(InventoryUiBhv inventory) {
            int count = 0;
            foreach (var slot in inventory.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (slot.IsOccupied) {
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
