using System;
using Assets.Code.Game;
using Assets.Code.Inn;
using Assets.Code.UI;
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
        private readonly Action<string, bool> _speak;
        private readonly TraditionalNavigator _navigator;
        private readonly ItemGrab _grab;
        private SubScreenCollectionBhv _collection;
        private InventoryUiBhv _inventory;
        private Container _root;
        private Container _heroes;
        private Container _buttons;
        private Container _items;
        private int _builtButtonsSignature;
        private int _builtItemsSignature;

        public InnScreen(Action<string, bool> speak, TraditionalNavigator navigator) {
            _speak = speak;
            _navigator = navigator;
            _grab = new ItemGrab(speak);
        }

        /// <summary>The grab key (Space / Shift+Space), routed here while this screen stands.</summary>
        public void ToggleGrab(Core.Nav.UIElement current, bool takeOne) => _grab.Toggle(current, takeOne);

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
            _grab.Reset();
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => {
                    if (_grab.Armed) {
                        _grab.Cancel();
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

            _buttons = new Container(ContainerShape.VerticalList);
            _root.Add(_buttons);
            PopulateButtons();

            // The inventory's frame (filter, count, wallet, sort) lives on persistent widgets,
            // so these elements are stable across re-sorts; only the pooled item slots below
            // ever need a rebuild. That keeps focus on Sort alive through its own press.
            if (_inventory != null) {
                // The filter reads as a tab: Left/Right apply the game's own tab buttons (they
                // are icon-only and invisible to a text sweep). Hidden tabs (HideIfEmpty) drop
                // out of the live list, mirroring the game's own cycling.
                var inventoryUi = _inventory;
                System.Func<System.Collections.Generic.List<InventoryFilterBhv>> tabs = () => {
                    var list = new System.Collections.Generic.List<InventoryFilterBhv>();
                    if (inventoryUi != null) {
                        list.AddRange(inventoryUi.GetComponentsInChildren<InventoryFilterBhv>(includeInactive: false));
                    }
                    return list;
                };
                _root.Add(new TabSelectorElement(
                    () => tabs().IndexOf(inventoryUi.CurrentFilter),
                    () => tabs().Count,
                    index => {
                        var list = tabs();
                        return index >= 0 && index < list.Count ? GameLoc.TryGet(list[index].GetTitleLocKey()) : null;
                    },
                    index => {
                        var list = tabs();
                        if (index >= 0 && index < list.Count) {
                            inventoryUi.ApplyFilter(list[index]);
                        }
                    }));
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
            PopulateItems(repairFocus: false);
            return _root;
        }

        public override bool OnUpdate(object target) {
            if (ButtonsSignature() != _builtButtonsSignature) {
                PopulateButtons();
            }
            if (_inventory != null && ItemsSignature() != _builtItemsSignature) {
                PopulateItems(repairFocus: true);
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
        private void PopulateItems(bool repairFocus) {
            var focused = repairFocus ? _navigator.Current : null;
            var focusedSlot = (focused as InventoryItemElement)?.Slot;
            bool onFreeSlots = focused is FreeSlotsElement;

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
            var inventoryUi = _inventory;
            _items.Add(new FreeSlotsElement(() => inventoryUi == null
                ? null : inventoryUi.GetComponentInChildren<InventoryItemContainerBhv>(includeInactive: false)));
            _builtItemsSignature = ItemsSignature();

            // The rebuild replaced our elements over the same live widgets; re-home focus over
            // the one it sat on so a sale, a placement, or a restock does not throw the cursor
            // to the top of the screen.
            if (focusedSlot != null || onFreeSlots) {
                foreach (var child in _items.Children) {
                    if ((focusedSlot != null && child is InventoryItemElement item && item.Slot == focusedSlot)
                        || (onFreeSlots && child is FreeSlotsElement)) {
                        if (child.CanFocus) {
                            _navigator.Focus(child, announce: false);
                        }
                        break;
                    }
                }
            }
        }

        // A wallet row ("Relics, 40"): the caption is the row's tooltip, the amount its label.
        // Shared with the inn station screens, which show the same wallet.
        internal static string CurrencyLine(Transform row) {
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
            // The disabled embark button nests a clickable "Select a Route" overlay; a nested
            // selectable is an input shim over its parent widget, which already reads it (the
            // overlay's tooltip surfaces in the parent's buffer), so only top-level widgets
            // become elements.
            var parent = selectable.transform.parent;
            if (parent != null && parent.GetComponentInParent<Selectable>() != null) {
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
