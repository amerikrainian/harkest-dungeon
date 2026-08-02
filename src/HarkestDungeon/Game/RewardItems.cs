using Assets.Code.Item;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using HarmonyLib;

namespace DD2A11y.Game {
    /// <summary>
    /// Reads the game's uninteractable reward icons (kingdom cell/event panel rewards): the
    /// icon's only visible text is its quantity badge, so the item's name comes from the
    /// widget's model - an instance, or a bare definition on a preview - composed like an
    /// inventory slot: title, then the stack size.
    /// </summary>
    public static class RewardItems {
        private static readonly AccessTools.FieldRef<UninteractableRewardItemBhv, ItemDefinition> DefinitionField =
            AccessTools.FieldRefAccess<UninteractableRewardItemBhv, ItemDefinition>("m_itemDefinition");

        /// <summary>The item's own title, falling back to the widget's visible text for a
        /// widget holding neither model.</summary>
        public static string Title(UninteractableRewardItemBhv reward) {
            var item = reward.Item;
            if (ItemUtils.IsValid(item)) {
                return ItemDescription.GetTitle(item.GetItemDefinition());
            }
            var definition = DefinitionField(reward);
            if (definition != null) {
                return ItemDescription.GetTitle(definition);
            }
            return UiText.AllText(reward.gameObject);
        }

        /// <summary>The stack size, spoken only past one; a definition-only preview has none.</summary>
        public static string Quantity(UninteractableRewardItemBhv reward) {
            var item = reward.Item;
            if (!ItemUtils.IsValid(item)) {
                return null;
            }
            int quantity = item.GetQty();
            return quantity > 1 ? quantity.ToString() : null;
        }
    }
}
