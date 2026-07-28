using System;
using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Items;
using Assets.Code.UI.Screens;
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
    /// The player-inventory panel, shared by every surface that shows it (the inn hub reads it
    /// inline, the standalone inventory screen is nothing else): the filter as a tab
    /// (Left/Right apply the game's own icon-only tab buttons; hidden tabs drop out of the
    /// live list), slot count and wallet readouts, the sort button (its press confirms
    /// "sorted by type" - the game's one sort), one element per carried item (title and
    /// stack, full tooltip in the buffer; Shift+Enter discards - or sells one, with a seller
    /// open), the free capacity as one collapsed line, and Space grab-and-place.
    /// </summary>
    internal sealed class InventoryPanel {
        private readonly Action<string, bool> _speak;
        private readonly TraditionalNavigator _navigator;
        private readonly ItemGrab _grab;
        private InventoryUiBhv _inventory;
        private Container _items;
        private int _builtItemsSignature;

        public InventoryPanel(Action<string, bool> speak, TraditionalNavigator navigator) {
            _speak = speak;
            _navigator = navigator;
            _grab = new ItemGrab(speak);
        }

        public bool GrabArmed => _grab.Armed;
        public void CancelGrab() => _grab.Cancel();
        public void ToggleGrab(UIElement current, bool takeOne) => _grab.Toggle(current, takeOne);

        /// <summary>Builds the panel into the surface's root: the frame first (its elements sit
        /// on persistent widgets, stable across re-sorts - focus on Sort survives its own
        /// press), then the pooled item list.</summary>
        public void BuildInto(Container root, InventoryUiBhv inventory) {
            _grab.Reset();
            _inventory = inventory;
            var inventoryUi = inventory;

            Func<List<InventoryFilterBhv>> tabs = () => {
                var list = new List<InventoryFilterBhv>();
                if (inventoryUi != null) {
                    list.AddRange(inventoryUi.GetComponentsInChildren<InventoryFilterBhv>(includeInactive: false));
                }
                return list;
            };
            root.Add(new TabSelectorElement(
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

            var count = FindChild(inventory.transform, "SlotCountContainer");
            if (count != null) {
                root.Add(new ReadoutElement(() => {
                    string text = count == null ? null : UiText.AllText(count.gameObject);
                    return string.IsNullOrEmpty(text) ? null : S.InventorySlots(text);
                }));
            }
            var currencies = FindChild(inventory.transform, "Currencies");
            if (currencies != null) {
                foreach (Transform row in currencies) {
                    var captured = row;
                    root.Add(new ReadoutElement(() => CurrencyLine(captured)));
                }
            }
            foreach (var selectable in inventory.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable.GetComponent<InventoryItemBhv>() != null || !Include(selectable)) {
                    continue;
                }
                var button = selectable;
                if (button.gameObject.name == "SortButton") {
                    root.Add(new ActionElement(() => UiText.FirstLabel(button.gameObject), S.RoleButton, () => {
                        ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current),
                            ExecuteEvents.submitHandler);
                        _speak(S.InventorySorted, false);
                    }));
                } else {
                    root.Add(new SelectableElement(button));
                }
            }

            _items = new Container(ContainerShape.VerticalList);
            root.Add(_items);
            PopulateItems(repairFocus: false);
        }

        /// <summary>Per-frame: the game's pooled slots recycle into brand-new instances on any
        /// change, so an identity signature drives the rebuild and focus is re-homed onto the
        /// slot it sat on.</summary>
        public void Update() {
            if (_inventory != null && ItemsSignature() != _builtItemsSignature) {
                PopulateItems(repairFocus: true);
            }
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

        internal static bool Include(Selectable selectable) {
            if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                return false;
            }
            // A nested selectable is an input shim over its parent widget, which already reads
            // it (the overlay's tooltip surfaces in the parent's buffer), so only top-level
            // widgets become elements.
            var parent = selectable.transform.parent;
            if (parent != null && parent.GetComponentInParent<Selectable>() != null) {
                return false;
            }
            return UiText.HasAnyTextSource(selectable.gameObject);
        }

        internal static Transform FindChild(Transform root, string name) {
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
