using System;
using Assets.Code.Data;
using Assets.Code.UI;
using Assets.Code.UI.Items;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The kingdoms Inn Storage screen (an <c>InnStorageBhv</c> stack entry over the player
    /// bag): the storage's own item list first - one element per stored stack, free capacity
    /// as one line - then the full bag panel beneath (filter, wallet, sort, items), both
    /// sharing one grab: Space carries stacks between storage and bag through the game's own
    /// swap model. Escape drops a held grab first, then closes.
    /// </summary>
    public sealed class InnStorageScreen : GameScreen {
        private readonly InventoryPanel _panel;
        private readonly TraditionalNavigator _navigator;
        private InnStorageBhv _storage;
        private Container _storageItems;
        private Container _root;
        private int _storageSignature;

        public InnStorageScreen(Action<string, bool> speak, TraditionalNavigator navigator) {
            _panel = new InventoryPanel(speak, navigator);
            _navigator = navigator;
        }

        /// <summary>The grab keys, routed here while this screen stands - one grab spans both
        /// inventories.</summary>
        public void ToggleGrab(UIElement current, bool takeOne) => _panel.ToggleGrab(current, takeOne);

        public override string Name {
            get {
                var screen = _storage == null ? null : _storage.GetComponentInParent<UiScreenBhv>();
                string title = screen == null ? null : UiText.FirstLabel(screen.gameObject);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _storage = top == null ? null : top.GetComponentInChildren<InnStorageBhv>(false);
            return _storage;
        }

        public override Container BuildRoot(object target) {
            var storage = (InnStorageBhv)target;
            var screen = storage.GetComponentInParent<UiScreenBhv>();
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (_panel.GrabArmed) {
                    _panel.CancelGrab();
                    return;
                }
                screen.TryCloseScreen();
            });
            var context = storage.GetComponent<DataContextBhv>();
            if (context != null) {
                _root.Add(new ReadoutElement(() => context == null ? null : context.GetStringValue("inn_name")));
            }
            _storageItems = new Container(ContainerShape.VerticalList);
            _root.Add(_storageItems);
            PopulateStorage(repairFocus: false);
            // The frame's own labeled buttons (the Inventory toggle over to the bag view).
            foreach (var selectable in storage.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable.GetComponent<InventoryItemBhv>() == null && InventoryPanel.Include(selectable)) {
                    _root.Add(new SelectableElement(selectable));
                }
            }
            // The bag screen stays open beneath storage; its shared panel reads it in full,
            // and the one grab spans both lists.
            var bag = SingletonMonoBehaviour<CommonUiBhv>.Instance.GetActiveInventoryInstance();
            var bagUi = bag == null ? null : bag.GetWidget<InventoryUiBhv>();
            if (bagUi != null) {
                _panel.BuildInto(_root, bagUi);
            }
            return _root;
        }

        public override bool OnUpdate(object target) {
            if (StorageSignature() != _storageSignature) {
                PopulateStorage(repairFocus: true);
            }
            _panel.Update();
            return false;
        }

        // The storage half mirrors the bag panel's item list: occupied pooled slots plus one
        // live free-capacity line, focus re-homed across the recycle.
        private void PopulateStorage(bool repairFocus) {
            var focusedSlot = repairFocus ? (_navigator.Current as InventoryItemElement)?.Slot : null;
            bool onFreeSlots = repairFocus && _navigator.Current is FreeSlotsElement
                && _navigator.Current.Parent == _storageItems;

            _storageItems.Clear();
            if (_storage == null) {
                _storageSignature = 0;
                return;
            }
            foreach (var slot in _storage.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (slot.IsOccupied) {
                    var selectable = slot.GetComponent<Selectable>();
                    if (selectable != null) {
                        _storageItems.Add(new InventoryItemElement(slot, selectable));
                    }
                }
            }
            var storage = _storage;
            _storageItems.Add(new FreeSlotsElement(() => storage == null
                ? null : storage.GetComponentInChildren<InventoryItemContainerBhv>(includeInactive: false)));
            _storageSignature = StorageSignature();

            if (focusedSlot != null || onFreeSlots) {
                foreach (var child in _storageItems.Children) {
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

        private int StorageSignature() {
            int signature = 17;
            if (_storage == null) {
                return 0;
            }
            foreach (var slot in _storage.GetComponentsInChildren<PlayerInventoryItemBhv>(includeInactive: false)) {
                if (slot.IsOccupied) {
                    signature = signature * 31 + slot.GetInstanceID();
                }
            }
            return signature;
        }
    }
}
