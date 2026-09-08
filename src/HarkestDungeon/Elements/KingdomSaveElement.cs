using System.Collections.Generic;
using Assets.Code.Kingdom;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// One saved kingdom in the save-select list: the save's name, then the widget's own day,
    /// difficulty and map labels, then the enemy gang by the game's campaign title for it (the
    /// row shows the gang as its banner and flag). Enter loads the save (the widget's click path
    /// selects and loads in one step); Shift+Enter opens the game's delete confirmation, the
    /// same path as the row's Delete button, which follows the row in the tree.
    /// </summary>
    public sealed class KingdomSaveElement : UIElement {
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, TMP_Text> DayLabelField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, TMP_Text>("m_DayLabel");
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, TMP_Text> DifficultyLabelField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, TMP_Text>("m_DifficultyLabel");
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, TMP_Text> MapLabelField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, TMP_Text>("m_MapLabel");
        // The list the row belongs to keeps every save's stats by directory; the row itself
        // keeps only the gang's sprites.
        private static readonly AccessTools.FieldRef<KingdomSaveItemBhv, KingdomSaveSelectUIBhv> SaveSelectField =
            AccessTools.FieldRefAccess<KingdomSaveItemBhv, KingdomSaveSelectUIBhv>("m_saveSelect");
        private static readonly AccessTools.FieldRef<KingdomSaveSelectUIBhv, Dictionary<string, KingdomSaveStats>> StatsField =
            AccessTools.FieldRefAccess<KingdomSaveSelectUIBhv, Dictionary<string, KingdomSaveStats>>("m_KingdomSaveStats");

        private readonly KingdomSaveItemBhv _item;

        public KingdomSaveElement(KingdomSaveItemBhv item) {
            _item = item;
        }

        public override bool CanFocus => _item != null && _item.gameObject.activeInHierarchy;

        public override string Label => _item.SaveName;

        public override string Role => S.RoleButton;

        public override string Value => SpokenLine.Join(
            Text(DayLabelField(_item)), Text(DifficultyLabelField(_item)), Text(MapLabelField(_item)),
            KingdomGangs.Title(Gang()));

        private static string Text(TMP_Text label) => label != null ? label.text : null;

        private string Gang() {
            var select = SaveSelectField(_item);
            string directory = _item.DirectoryName;
            if (select == null || directory == null) {
                return null;
            }
            var stats = StatsField(select);
            return stats != null && stats.TryGetValue(directory, out var save) ? save.m_Gang : null;
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _item.OnClick);
            yield return new ElementAction("discard", _item.OnDeleteSavePressed);
        }
    }
}
