using System.Collections.Generic;
using Assets.Code.Audio;
using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.UI.Managers;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using UnityEngine;
using UnityEngine.UI;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// An equipment slot on a sheet (trinket, combat item, coach equipment): the label is the
    /// equipped item's own title read from the model - current the same frame a swap lands,
    /// where the widget's text is a frame late - or the slot's caption while empty ("Equip
    /// Trinket"; an empty coach slot is named from its accepted type instead, "Trophy, empty
    /// slot"). Enter runs
    /// the slot's own submit (equip through the game's slot-select, unequip through its
    /// auto-transfer) and the landed state is spoken back - except an occupied slot on the road
    /// with the bag closed, where the game's submit only opens the bag (the first click of its
    /// two-click mouse flow, which would carry focus away from the sheet): there Enter performs
    /// the bag-open transfer of the game's auto-transfer directly, so one press unequips and
    /// the slot's re-read is the feedback. An empty slot pressed while the bag already stands
    /// open beneath the sheet (the inn hub keeps it there) reports the game's response through
    /// <c>onBagBrowse</c>: the submit filters the open bag to the slot's type and moves the
    /// game's own selection into it without pushing any screen, so the screen - not routing -
    /// must carry focus into the bag. The full item tooltip stays in the buffer.
    /// </summary>
    public sealed class EquipSlotElement : SelectableElement {
        private readonly InventoryItemBhv _slot;
        private readonly System.Action<ItemType> _onBagBrowse;
        private bool _openedBag;

        public EquipSlotElement(InventoryItemBhv slot, Selectable selectable, GameObject rowScope,
                System.Action<ItemType> onBagBrowse = null)
            : base(selectable, null, rowScope) {
            _slot = slot;
            _onBagBrowse = onBagBrowse;
        }

        public override string Label {
            get {
                var item = _slot != null ? _slot.Item : null;
                if (ItemUtils.IsValid(item)) {
                    return ItemDescription.GetTitle(item.GetItemDefinition());
                }
                return CoachEmptyLabel() ?? base.Label;
            }
        }

        // An empty coach slot's visible text never says it is empty: the general slots show
        // the bare item-type caption ("Stagecoach Item") and the trophy slot draws only its
        // hint tooltip, which the widget-text fallback would read as the label. Name the slot
        // from its container's accepted type and say it is empty; the hint text stays a buffer
        // line. The game's own empty_item flag excludes locked slots, whose "New Slot" caption
        // stands.
        private string CoachEmptyLabel() {
            if (!(_slot is InventoryItemStageCoachUpgradeBhv)
                || !_slot.GetComponent<DataContextBhv>().GetBoolValue("empty_item")) {
                return null;
            }
            var container = (InventoryItemContainerStageCoachUpgradeBhv)_slot.ItemContainer;
            string word = container.SlotType == ItemSlotType.GENERAL
                ? GameLoc.TryGet("item_type_sc_upgrade")
                : GameLoc.TryGet("stat_source_type_" + container.SlotType.m_SourceType);
            return SpokenLine.Join(word, S.EmptySlot);
        }

        // A press that handed play to the bag must not re-read the slot: the bag landing that
        // follows is the feedback.
        public override bool ReannounceOnActivate => !_openedBag;

        public override string GetValueText() => Label;

        /// <summary>Whether the game's armed pick holds an item this slot's row can receive -
        /// the game marks the destination container by handing it the held item as its
        /// SelectedItem when the pick begins.</summary>
        public bool PickDestination => _slot != null && _slot.ItemContainer.SelectedItem != null;

        public bool Occupied => _slot != null && ItemUtils.IsValid(_slot.Item);

        /// <summary>The coach sheet's altar-locked slots, stamped by the game into the slot's
        /// DataContext; other sheets' slots carry no context and read unlocked.</summary>
        public bool Locked {
            get {
                var context = _slot == null ? null : _slot.GetComponent<DataContextBhv>();
                return context != null && context.GetBoolValue("locked_item");
            }
        }

        // A slot the armed pick can land on: the game greys the coach slots' Selectable for
        // the pick's duration while showing them glowing as destinations, and its own submit
        // completes the placement in that state regardless of the interactable flag.
        private bool ArmedDestination => PickDestination && !Locked;

        public override string Status => ArmedDestination ? null : base.Status;

        public override IEnumerable<ElementAction> GetActions() {
            bool any = false;
            foreach (var action in base.GetActions()) {
                any = true;
                yield return action.Id == ActionIds.Activate
                    ? new ElementAction(ActionIds.Activate, Activate)
                    : action;
            }
            if (!any && ArmedDestination) {
                yield return new ElementAction(ActionIds.Activate, Activate);
            }
        }

        private void Activate() {
            _openedBag = false;
            if (RoadBaglessUnequip()) {
                Unequip();
                return;
            }
            bool browse = BagBrowse();
            Submit();
            if (browse) {
                _openedBag = true;
                _onBagBrowse(_slot is TrinketInventoryItemBhv ? ItemType.TRINKET : ItemType.COMBAT);
            }
        }

        // The game's empty-slot submit branch when the bag is already open: OnSelected filters
        // the standing bag to this slot's type and moves the game's own selection into it - no
        // screen is pushed, so only the screen can follow. (Bag closed, the same press pushes
        // the bag screen and routing flips to it.) Mirrors every guard the game's OnSelected
        // checks, read before the submit runs; an armed pick's press is a placement instead.
        private bool BagBrowse() {
            if (_onBagBrowse == null || Occupied
                || !(_slot is TrinketInventoryItemBhv || _slot is CombatInventoryItemBhv)) {
                return false;
            }
            var mode = GameModeMgr.CurrentMode;
            if (mode == GameModeType.COMBAT || mode == GameModeType.ALTAR_OF_HOPE
                || mode == GameModeType.HERO_SELECT) {
                return false;
            }
            var common = SingletonMonoBehaviour<CommonUiBhv>.Instance;
            if (_slot is TrinketInventoryItemBhv && common.IsStoryScreenActive) {
                return false;
            }
            return common.IsInventoryActive
                && !common.IsSelectingItemSlot
                && !common.IsInnHeroReplacementScreenActive()
                && !common.IsInnStorageActive;
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
