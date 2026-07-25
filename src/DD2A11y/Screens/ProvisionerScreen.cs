using Assets.Code.UI.Items;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn's item store (the Provisioner, an <c>InnStoreUiBhv</c> stack entry), named by
    /// the inn header's own station title. Layout: the wallet rows, then the store's slots
    /// ("Bear Trap, button, relic 6, 2" - Enter buys through the game's own purchase, a
    /// sold-out slot reads the game's "Out of Stock!"), then the player's bag - one element
    /// per carried item plus the free-capacity line - where Shift+Enter SELLS one item per
    /// press (the game's shift-click while a seller is open; spoken "sold X"). Escape closes
    /// the store through its own close flow.
    /// </summary>
    public sealed class ProvisionerScreen : GameScreen {
        private readonly TraditionalNavigator _navigator;
        private InnStoreUiBhv _store;
        private Container _root;
        private Container _slots;
        private Container _items;
        private int _builtSignature;

        public ProvisionerScreen(TraditionalNavigator navigator) {
            _navigator = navigator;
        }

        public override string Name => InnStations.Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _store = top == null ? null : top.GetComponent<InnStoreUiBhv>();
            return _store;
        }

        public override Container BuildRoot(object target) {
            var store = (InnStoreUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: store.CloseSubscreen);

            var bag = Object.FindObjectOfType<InventoryUiBhv>();
            if (bag != null) {
                var currencies = FindChild(bag.transform, "Currencies");
                if (currencies != null) {
                    foreach (Transform row in currencies) {
                        var captured = row;
                        _root.Add(new ReadoutElement(() => InnScreen.CurrencyLine(captured)));
                    }
                }
            }

            _slots = new Container(ContainerShape.VerticalList);
            _root.Add(_slots);
            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            Populate(store, bag, repairFocus: false);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var store = (InnStoreUiBhv)target;
            var bag = Object.FindObjectOfType<InventoryUiBhv>();
            if (Signature(store, bag) != _builtSignature) {
                Populate(store, bag, repairFocus: true);
            }
            return false;
        }

        // Both lists ride pooled widgets that recycle on every purchase or sale, so the
        // rebuild is keyed to an instance-id signature and focus is re-homed over the widget
        // it sat on (same pattern as the inn bag).
        private void Populate(InnStoreUiBhv store, InventoryUiBhv bag, bool repairFocus) {
            var focused = repairFocus ? _navigator.Current : null;
            var focusedStore = (focused as StoreItemElement)?.Slot;
            var focusedSlot = (focused as InventoryItemElement)?.Slot;
            bool onFreeSlots = focused is FreeSlotsElement;

            _slots.Clear();
            foreach (var slot in store.GetComponentsInChildren<StoreInventoryItemBhv>(includeInactive: false)) {
                var selectable = slot.GetComponent<Selectable>();
                if (selectable != null) {
                    _slots.Add(new StoreItemElement(slot, selectable));
                }
            }

            _items.Clear();
            if (bag != null) {
                foreach (var slot in bag.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                    if (slot.IsOccupied) {
                        var selectable = slot.GetComponent<Selectable>();
                        if (selectable != null) {
                            _items.Add(new InventoryItemElement(slot, selectable));
                        }
                    }
                }
                var bagUi = bag;
                _items.Add(new FreeSlotsElement(() => bagUi == null
                    ? null : bagUi.GetComponentInChildren<InventoryItemContainerBhv>(includeInactive: false)));
            }
            _builtSignature = Signature(store, bag);

            if (focusedStore != null || focusedSlot != null || onFreeSlots) {
                foreach (var container in new[] { _slots, _items }) {
                    foreach (var child in container.Children) {
                        bool match = (focusedStore != null && child is StoreItemElement s && s.Slot == focusedStore)
                            || (focusedSlot != null && child is InventoryItemElement item && item.Slot == focusedSlot)
                            || (onFreeSlots && child is FreeSlotsElement);
                        if (match && child.CanFocus) {
                            _navigator.Focus(child, announce: false);
                            return;
                        }
                    }
                }
            }
        }

        private static int Signature(InnStoreUiBhv store, InventoryUiBhv bag) {
            int signature = 17;
            foreach (var slot in store.GetComponentsInChildren<StoreInventoryItemBhv>(includeInactive: false)) {
                signature = signature * 31 + slot.GetInstanceID();
                signature = signature * 31 + (slot.IsOccupied ? 1 : 0);
            }
            if (bag != null) {
                foreach (var slot in bag.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                    if (slot.IsOccupied) {
                        signature = signature * 31 + slot.GetInstanceID();
                    }
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
