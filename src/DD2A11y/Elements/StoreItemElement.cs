using System.Collections.Generic;
using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One slot in a store (the inn's Provisioner): the item's own title with its price and
    /// remaining stock on the focus line ("Bear Trap, button, relic 6, 2"), the full item
    /// tooltip in the buffer. A sold-out slot reads the game's own "Out of Stock!" label.
    /// Enter runs the game's buy (validated by the game; an unchanged stock answers with the
    /// game's insufficient-funds line).
    /// </summary>
    public sealed class StoreItemElement : SelectableElement {
        private readonly StoreInventoryItemBhv _slot;
        private readonly TMP_Text _price;

        /// <summary>The live slot widget, for focus re-homing across rebuilds.</summary>
        public StoreInventoryItemBhv Slot => _slot;

        public StoreItemElement(StoreInventoryItemBhv slot, Selectable selectable) : base(selectable) {
            _slot = slot;
            foreach (var tmp in slot.GetComponentsInChildren<TMP_Text>(includeInactive: false)) {
                if (tmp.gameObject.name == "Price") {
                    _price = tmp;
                    break;
                }
            }
        }

        public override string Label {
            get {
                var item = _slot.Item;
                // A sold-out slot's own label ("Out of Stock!") is its first text.
                return ItemUtils.IsValid(item) ? ItemDescription.GetTitle(item.GetItemDefinition()) : base.Label;
            }
        }

        public override string Value {
            get {
                var item = _slot.Item;
                if (!ItemUtils.IsValid(item)) {
                    return null;
                }
                int stock = item.GetQty();
                return SpokenLine.Join(_price == null ? null : _price.text,
                    stock > 1 ? stock.ToString() : null);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !ItemUtils.IsValid(_slot.Item)) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, () => {
                int before = _slot.Item == null ? 0 : _slot.Item.GetQty();
                _slot.OnTryBuyItem();
                var after = _slot.Item;
                if (after == null || after.GetQty() != before) {
                    SpeechPipeline.Instance?.Speak(GetFocusText()); // the slot's new state
                } else {
                    SpeechPipeline.Instance?.Speak(
                        GameLoc.TryGet("insufficient_funds_label") ?? S.StatusUnavailable, interrupt: true);
                }
            });
        }
    }
}
