using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One occupied item slot in any of the game's inventories (a loot reward, a bag slot at
    /// the inn): the item's own title with its stack size, the full item tooltip as buffer
    /// lines, and Enter through the game's own submit handler (take on the loot screen,
    /// auto-transfer or slot-select in a bag; the invalid click plays when it cannot act).
    /// Empty slots cannot take focus.
    /// </summary>
    public sealed class InventoryItemElement : SelectableElement {
        private readonly InventoryItemBhv _item;

        public InventoryItemElement(InventoryItemBhv item, Selectable selectable)
            : base(selectable, null, item.gameObject) {
            _item = item;
        }

        public override bool CanFocus => base.CanFocus && _item.IsOccupied;

        public override string Label {
            get {
                var item = _item.Item;
                if (!ItemUtils.IsValid(item)) {
                    return null;
                }
                return ItemDescription.GetTitle(item.GetItemDefinition());
            }
        }

        public override string Value {
            get {
                var item = _item.Item;
                if (!ItemUtils.IsValid(item)) {
                    return null;
                }
                int quantity = item.GetQty();
                return quantity > 1 ? quantity.ToString() : null;
            }
        }

        // Discard (Shift+Enter, the game's shift-click), advertised only where the game itself
        // allows it: a player bag slot holding a discardable item. The game's own handler runs
        // its trophy safeguards.
        public override System.Collections.Generic.IEnumerable<ElementAction> GetActions() {
            foreach (var action in base.GetActions()) {
                yield return action;
            }
            if (_item is PlayerInventoryItemBhv player && ItemUtils.IsValid(player.Item)
                && player.Item.GetItemDefinition().m_canDiscard) {
                yield return new ElementAction("discard", player.OnDiscardItem);
            }
        }
    }
}
