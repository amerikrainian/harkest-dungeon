using System;
using Assets.Code.Item;
using Assets.Code.Item.Events;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Grab-and-place for inventory stacks, the keyboard face of the game's item drag and
    /// drop: Space grabs the focused stack, and on a landing target Space places the whole
    /// stack while Shift+Space places a single item off it - the game's split-stack drag -
    /// keeping the grab held so repeated presses keep splitting until the stack runs out.
    /// Placement runs the same model calls as the game's own drop handler
    /// (<c>InventoryItemBhv.DefaultSwap</c>): a whole-stack place moves, swaps, or combines
    /// through <c>ItemInventory.SwapItems</c> - an occupied target swaps even with both
    /// inventories full. A <see cref="FreeSlotsElement"/> is a valid destination meaning
    /// that inventory's free space; a fresh stack created there goes to the LAST empty slot,
    /// so it reads adjacent to the capacity line the cursor sits on. The source is held as
    /// inventory + slot index - live model references revalidated at place time - so the
    /// pooled slot widgets can recycle underneath without stranding the grab.
    /// </summary>
    public sealed class ItemGrab {
        private readonly Action<string, bool> _speak;
        private ItemInventory _inventory;
        private int _index;
        private ItemDefinition _definition;

        public ItemGrab(Action<string, bool> speak) {
            _speak = speak;
        }

        public bool Armed => _inventory != null;

        public void Reset() {
            _inventory = null;
            _definition = null;
        }

        /// <summary>Drop an armed grab with feedback (the Escape path).</summary>
        public void Cancel() {
            Reset();
            _speak(S.GrabCancelled, true);
        }

        /// <summary>The grab keys: Space picks up the focused stack or places the whole held
        /// one; Shift+Space places a single item off the held stack (never initiates).</summary>
        public void Toggle(UIElement current, bool takeOne) {
            if (Armed) {
                Place(current, takeOne);
            } else if (takeOne) {
                _speak(S.StatusUnavailable, true); // nothing held to split from
            } else {
                Grab(current);
            }
        }

        // Only player bag / inn storage slots are grabbable; loot and store widgets keep
        // their own transfer flows on Enter.
        private void Grab(UIElement current) {
            var slot = (current as InventoryItemElement)?.Slot as PlayerInventoryItemBhv;
            if (slot == null || !ItemUtils.IsValid(slot.Item)) {
                return;
            }
            _inventory = slot.ItemContainer.Inventory;
            _index = slot.ItemIndex;
            _definition = slot.Item.GetItemDefinition();
            _speak(S.Grabbed(ItemDescription.GetTitle(_definition)), true);
        }

        private void Place(UIElement current, bool takeOne) {
            var source = _inventory.GetItemOrDefault(_index);
            if (!ItemUtils.IsValid(source) || !source.Is(_definition)) {
                Cancel(); // the source changed underneath (a sort, a sale) - stale grabs never fire
                return;
            }

            ItemInventory destination;
            int index;
            InventoryItemBhv acceptor; // any live widget of the destination container, the AcceptsItem oracle
            if (current is InventoryItemElement element && element.Slot is PlayerInventoryItemBhv target) {
                destination = target.ItemContainer.Inventory;
                index = target.ItemIndex;
                acceptor = target;
            } else if (current is FreeSlotsElement free
                       && free.Container != null && free.Container.Inventory != null) {
                destination = free.Container.Inventory;
                // Free space: a single first looks for an existing stack it can combine
                // into - skipping the source, which the game's own scan would return - so
                // repeated splits accumulate into one stack instead of scattering; anything
                // else opens the LAST empty slot, placing the new stack at the bottom of
                // the spoken list, right above the capacity line.
                index = -1;
                if (takeOne) {
                    for (int i = 0; i < destination.GetNumberOfTotalSlots(); i++) {
                        if (destination == _inventory && i == _index) {
                            continue;
                        }
                        var candidate = destination.GetItem(i);
                        if (ItemUtils.IsValid(candidate) && candidate.Is(_definition)
                            && candidate.IsCombinable()
                            && !candidate.IsMaxed(destination.GetIsRunStatModified())) {
                            index = i;
                            break;
                        }
                    }
                }
                if (index < 0) {
                    index = FindLastEmptySlot(destination);
                }
                acceptor = free.Container.GetElementCount() > 0 ? free.Container.GetElement(0) : null;
                if (index < 0 || acceptor == null) {
                    _speak(S.CannotPlace, true);
                    return;
                }
            } else {
                _speak(S.CannotPlace, true);
                return;
            }

            if (destination == _inventory && index == _index) {
                if (takeOne) {
                    _speak(S.CannotPlace, true); // a stack cannot split onto itself
                } else {
                    Cancel(); // placed back where it came from
                }
                return;
            }

            var held = destination.GetItemOrDefault(index);
            // Cross-inventory placement honors the game's own acceptance rules
            // (InventoryItemBhv.CanSwapWith: both sides must accept what they would receive -
            // this is what keeps undiscardable items out of inn storage). Within one
            // inventory the game accepts unconditionally.
            if (destination != _inventory) {
                var sourceSlot = FindSlot(_inventory, _index);
                if (sourceSlot == null) {
                    Plugin.Log.LogWarning("ItemGrab: source widget vanished for "
                        + _definition.m_id + " at " + _index);
                    Cancel();
                    return;
                }
                if (!acceptor.AcceptsItem(_definition)
                    || (ItemUtils.IsValid(held) && !sourceSlot.AcceptsItem(held.GetItemDefinition()))) {
                    _speak(S.CannotPlace, true);
                    return;
                }
            }

            if (takeOne) {
                // The game's take-one guard, minus its fallback: on a different-item target
                // DefaultSwap falls back to swapping the whole stacks, but a spoken split
                // must never move more than one item.
                bool fits = ItemUtils.IsEmpty(held)
                    || (held.Is(_definition) && !held.IsMaxed(destination.GetIsRunStatModified()));
                if (!fits) {
                    _speak(S.CannotPlace, true);
                    return;
                }
                var single = _inventory.TakeItemQty(_index, 1);
                if (single != null) {
                    destination.SwapItems(single, index);
                }
            } else {
                destination.SwapItems(_inventory, _index, index, isPurchase: false);
            }
            EventInventoryItemSwapped.Trigger(_definition, _inventory, destination);

            // The landing, read from the model (widget text is a frame late): the placed
            // stack's title and its new size. Composed before Reset clears the definition.
            var placed = destination.GetItemOrDefault(index);
            string line = ItemDescription.GetTitle(_definition);
            if (ItemUtils.IsValid(placed) && placed.Is(_definition) && placed.GetQty() > 1) {
                line = SpokenLine.Join(line, placed.GetQty().ToString());
            }
            // A split keeps the grab held for the next Shift+Space and ends only when the
            // source stack runs out; a whole-stack place always ends it.
            if (!takeOne || !ItemUtils.IsValid(_inventory.GetItemOrDefault(_index))) {
                Reset();
            }
            _speak(line, true);
        }

        private static int FindLastEmptySlot(ItemInventory inventory) {
            for (int i = inventory.GetNumberOfTotalSlots() - 1; i >= 0; i--) {
                if (ItemUtils.IsEmpty(inventory.GetItem(i))) {
                    return i;
                }
            }
            return -1;
        }

        private static PlayerInventoryItemBhv FindSlot(ItemInventory inventory, int index) {
            foreach (var container in UnityEngine.Object.FindObjectsOfType<InventoryItemContainerBhv>()) {
                if (container.Inventory == inventory) {
                    return container.FindItemBhvWithItemIndex(index) as PlayerInventoryItemBhv;
                }
            }
            return null;
        }
    }
}
