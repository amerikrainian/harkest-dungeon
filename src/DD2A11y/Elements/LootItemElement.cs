using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using DD2A11y.Game;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One reward on the loot screen: the item's own title with its stack size, the full item
    /// tooltip as buffer lines, and Enter taking it through the game's own submit handler (which
    /// also plays the invalid click when the player inventory is full).
    /// </summary>
    public sealed class LootItemElement : SelectableElement {
        private readonly InventoryItemBhv _item;

        public LootItemElement(InventoryItemBhv item, Selectable selectable)
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
    }
}
