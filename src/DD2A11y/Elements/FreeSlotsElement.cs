using System;
using Assets.Code.UI.Items;
using DD2A11y.Core.Nav;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// An inventory's free capacity collapsed to one live line ("15 empty slots"). Slot cells
    /// carry no gameplay meaning, so they are never listed; this line stands in for all of
    /// them, and doubles as the grab-and-place destination meaning "this inventory's free
    /// space". Hidden while the inventory is full.
    /// </summary>
    public sealed class FreeSlotsElement : UIElement {
        private readonly Func<InventoryItemContainerBhv> _container;

        public FreeSlotsElement(Func<InventoryItemContainerBhv> container) {
            _container = container;
        }

        /// <summary>The live slot container this line summarizes, for the grab flow.</summary>
        public InventoryItemContainerBhv Container => _container();

        public override bool CanFocus => EmptyCount > 0;

        public override string Label {
            get {
                int count = EmptyCount;
                return count > 0 ? S.InventoryEmptySlots(count) : null;
            }
        }

        private int EmptyCount {
            get {
                var container = Container;
                return container == null || container.Inventory == null
                    ? 0 : container.Inventory.GetNumberOfEmptySlots();
            }
        }
    }
}
