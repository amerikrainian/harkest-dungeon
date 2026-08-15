using System.Collections.Generic;
using Assets.Code.Audio;
using Assets.Code.Game;
using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.UI.Managers;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// An equipment slot on a sheet (trinket, combat item): the label is the equipped item's
    /// own title read from the model - current the same frame a swap lands, where the widget's
    /// text is a frame late - or the slot's caption while empty ("Equip Trinket"). Enter runs
    /// the slot's own submit (equip through the game's slot-select, unequip through its
    /// auto-transfer) and the landed state is spoken back - except an occupied slot on the road
    /// with the bag closed, where the game's submit only opens the bag (the first click of its
    /// two-click mouse flow, which would carry focus away from the sheet): there Enter performs
    /// the bag-open transfer of the game's auto-transfer directly, so one press unequips and
    /// the slot's re-read is the feedback. The full item tooltip stays in the buffer.
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

        public override IEnumerable<ElementAction> GetActions() {
            foreach (var action in base.GetActions()) {
                yield return action.Id == ActionIds.Activate
                    ? new ElementAction(ActionIds.Activate, Activate)
                    : action;
            }
        }

        private void Activate() {
            if (RoadBaglessUnequip()) {
                Unequip();
            } else {
                Submit();
            }
        }

        // The game's own submit branch that swallows the press: an occupied trinket/combat-item
        // slot while driving with the bag closed only opens the bag (InventoryItemBhv.OnSubmit),
        // and the unequip needs every guard its OnTryAutoTransfer checks besides the bag.
        private bool RoadBaglessUnequip() {
            if (GameModeMgr.CurrentMode != GameModeType.DRIVING
                || !(_slot is TrinketInventoryItemBhv || _slot is CombatInventoryItemBhv)) {
                return false;
            }
            var common = SingletonMonoBehaviour<CommonUiBhv>.Instance;
            var item = _slot.Item;
            return !common.IsInventoryActive
                && ItemUtils.IsValid(item)
                && !item.GetItemDefinition().m_IsUnequipInvalid
                && common.IsCharacterSheetActiveAndInventoryEditable
                && common.IsCharacterSheetActiveAndNotClosing;
        }

        // The transfer the slot's OnTryAutoTransfer performs once the bag is open, sounds
        // included: slot to player inventory, or the game's refusal click when the bag is full.
        private void Unequip() {
            var definition = _slot.Item.GetItemDefinition();
            int qty = _slot.Item.GetQty();
            var playerInventory = Singleton<GameTypeMgr>.Instance.PlayerInventory;
            var audio = SingletonMonoBehaviour<AudioMgr>.Instance;
            if (playerInventory.CanAdd(definition, qty)) {
                playerInventory.AddItems(definition, qty, isPurchase: false);
                _slot.ItemContainer.Inventory.TakeItemQty(_slot.ItemIndex, qty);
                _slot.Refresh();
                audio.Play(_slot is TrinketInventoryItemBhv
                    ? AudioPathsBhv.TrinketUnequip : AudioPathsBhv.CombatItemUnequip);
            } else {
                audio.Play(AudioPathsBhv.ClickInvalid);
            }
        }
    }
}
