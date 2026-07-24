using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// An equipment slot on a sheet (trinket, combat item): the label is the equipped item's
    /// own title read from the model - current the same frame a swap lands, where the widget's
    /// text is a frame late - or the slot's caption while empty ("Equip Trinket"). Enter runs
    /// the slot's own submit (equip through the game's slot-select, unequip through its
    /// auto-transfer) and the landed state is spoken back. The full item tooltip stays in the
    /// buffer.
    /// </summary>
    public sealed class EquipSlotElement : SelectableElement {
        private readonly InventoryItemBhv _slot;

        public EquipSlotElement(InventoryItemBhv slot, Selectable selectable, GameObject rowScope)
            : base(selectable, null, rowScope) {
            _slot = slot;
        }

        public override string Label {
            get {
                var item = _slot != null ? _slot.Item : null;
                return ItemUtils.IsValid(item) ? ItemDescription.GetTitle(item.GetItemDefinition()) : base.Label;
            }
        }

        public override bool ReannounceOnActivate => true;

        public override string GetValueText() => Label;
    }
}
