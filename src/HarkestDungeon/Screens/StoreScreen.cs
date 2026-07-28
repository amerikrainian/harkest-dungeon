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
    /// An item store: the inn's Provisioner (an <c>InnStoreUiBhv</c> stack entry, named by the
    /// inn header's station title) or a road merchant like the Hoarder (a <c>StoreUiBhv</c>
    /// screen named by its own title, which raises the player inventory panel on top of
    /// itself). Layout either way: the wallet rows, then the store's slots ("Bear Trap,
    /// button, relic 6, 2" - Enter buys through the game's own purchase, a sold-out slot
    /// reads the game's "Out of Stock!"), then the player's bag - one element per carried
    /// item plus the free-capacity line - where Shift+Enter SELLS one item per press where
    /// the game allows selling (spoken "sold X"). Escape closes the store through its own
    /// close flow (the inn's subscreen pop, the road store's done button).
    /// </summary>
    public sealed class StoreScreen : GameScreen {
        private readonly TraditionalNavigator _navigator;
        private Component _store;
        private Container _root;
        private Container _wallet;
        private Container _slots;
        private Container _items;
        private int _builtSignature;

        public StoreScreen(TraditionalNavigator navigator) {
            _navigator = navigator;
        }

        public override string Name {
            get {
                if (_store is StoreUiBhv road) {
                    var screen = road.GetComponentInParent<UiScreenBhv>();
                    var anchor = FindChild(screen != null ? screen.transform : road.transform, "exit_anchor");
                    string title = anchor == null ? null : UiText.FirstLabel(anchor.gameObject);
                    if (!string.IsNullOrEmpty(title)) {
                        return title;
                    }
                }
                return InnStations.Title() ?? S.ScreenGeneric;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            if (top == null) {
                _store = null;
                return null;
            }
            _store = (Component)top.GetComponent<InnStoreUiBhv>() ?? top.GetComponentInChildren<StoreUiBhv>(includeInactive: false);
            if (_store == null && top.GetComponent<InventoryUiBhv>() != null) {
                // A road store raises the player inventory panel ABOVE itself; the pair reads
                // as the one store surface.
                _store = Object.FindObjectOfType<StoreUiBhv>();
            }
            return _store;
        }

        public override Container BuildRoot(object target) {
            var store = (Component)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => Close(store));
            _wallet = new Container(ContainerShape.VerticalList);
            _root.Add(_wallet);
            _slots = new Container(ContainerShape.VerticalList);
            _root.Add(_slots);
            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            Populate(store, Object.FindObjectOfType<InventoryUiBhv>(), repairFocus: false);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var store = (Component)target;
            var bag = Object.FindObjectOfType<InventoryUiBhv>();
            if (Signature(store, bag) != _builtSignature) {
                Populate(store, bag, repairFocus: true);
            }
            return false;
        }

        private static void Close(Component store) {
            if (store is InnStoreUiBhv inn) {
                inn.CloseSubscreen();
            } else if (store is StoreUiBhv road) {
                road.HandleDoneButton();
            }
        }

        // Both lists ride pooled widgets that recycle on every purchase or sale, so the
        // rebuild is keyed to an instance-id signature and focus is re-homed over the widget
        // it sat on (same pattern as the inn bag).
        private void Populate(Component store, InventoryUiBhv bag, bool repairFocus) {
            var focused = repairFocus ? _navigator.Current : null;
            var focusedStore = (focused as StoreItemElement)?.Slot;
            var focusedSlot = (focused as InventoryItemElement)?.Slot;
            bool onFreeSlots = focused is FreeSlotsElement;

            // The wallet lives on the player inventory panel, which a road store raises a
            // beat AFTER its own screen - so the rows ride the rebuild, not the entry build.
            _wallet.Clear();
            if (bag != null) {
                var currencies = FindChild(bag.transform, "Currencies");
                if (currencies != null) {
                    foreach (Transform row in currencies) {
                        var captured = row;
                        _wallet.Add(new ReadoutElement(() => InventoryPanel.CurrencyLine(captured)));
                    }
                }
            }

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

        private static int Signature(Component store, InventoryUiBhv bag) {
            int signature = bag != null ? 19 : 17;
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
