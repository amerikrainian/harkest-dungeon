using System.Collections.Generic;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// One saved kingdom in the save-select list: the save's name, then the widget's own day,
    /// difficulty and map labels. Enter loads the save (the widget's click path selects and
    /// loads in one step); Shift+Enter opens the game's delete confirmation.
    /// </summary>
    public sealed class KingdomSaveElement : UIElement {
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, TMP_Text> DayLabelField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, TMP_Text>("m_DayLabel");
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, TMP_Text> DifficultyLabelField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, TMP_Text>("m_DifficultyLabel");
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, TMP_Text> MapLabelField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, TMP_Text>("m_MapLabel");

        private readonly KingdomSaveItemBhv _item;

        public KingdomSaveElement(KingdomSaveItemBhv item) {
            _item = item;
        }

        public override bool CanFocus => _item != null && _item.gameObject.activeInHierarchy;

        public override string Label => _item.SaveName;

        public override string Role => S.RoleButton;

        public override string Value => SpokenLine.Join(
            Text(DayLabelField(_item)), Text(DifficultyLabelField(_item)), Text(MapLabelField(_item)));

        private static string Text(TMP_Text label) => label != null ? label.text : null;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _item.OnClick);
            yield return new ElementAction("discard", _item.OnDeleteSavePressed);
        }
    }
}
