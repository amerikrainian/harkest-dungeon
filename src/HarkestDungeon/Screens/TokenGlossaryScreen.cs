using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// The token glossary (a stack entry over pause, combat, the inn, ...), named by the pause
    /// menu's own caption for it. The game shows a flat token list whose name colours encode
    /// the category, decoded by an on-screen legend; here each category is one labeled
    /// horizontal row (Left/Right within, Up/Down across) holding its tokens in the game's
    /// own order. Tokens are plain entries - the game wires no action to them; their
    /// Selectable is only a controller scroll anchor - with the token's description as
    /// buffer lines. Escape closes through the game's own teardown.
    /// </summary>
    public sealed class TokenGlossaryScreen : GameScreen {
        private static readonly AccessTools.FieldRef<TokenGlossaryWidgetBhv, List<GameObject>> RowsField =
            AccessTools.FieldRefAccess<TokenGlossaryWidgetBhv, List<GameObject>>("m_tokenObjectsAdded");

        // Group colour key -> legend caption key, the pairing the on-screen legend draws (its
        // pips are styled shades of these colours). The game colours hero tokens with
        // special's exact hex and biome tokens with unique's, so those groups fold into the
        // same captions here just as they do visually.
        private static readonly (string ColorKey, string LabelKey)[] Groups = {
            ("glossary_buff", "buffs_label"),
            ("glossary_stealth", "glossary_stealth_label"),
            ("glossary_debuff", "debuffs_label"),
            ("glossary_other", "glossary_other_label"),
            ("glossary_special", "glossary_special_label"),
            ("glossary_unique", "glossary_enemy_type_label"),
        };

        private TokenGlossaryWidgetBhv _widget;

        public override string Name => GameLoc.TryGet("pause_menu_glossary") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<TokenGlossaryWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (TokenGlossaryWidgetBhv)target;
            var root = new RootContainer(ContainerShape.VerticalList,
                back: () => SingletonMonoBehaviour<CommonUiBhv>.Instance.HideTokenGlossary());
            Container section = null;
            string sectionLabel = null;
            foreach (var row in RowsField(widget)) {
                var context = row.GetComponent<DataContextBhv>();
                string category = CategoryOf(context.GetColorValue("name_colour"));
                if (section == null || category != sectionLabel) {
                    section = new Container(ContainerShape.HorizontalList, category);
                    root.Add(section);
                    sectionLabel = category;
                }
                var capturedRow = row;
                var capturedContext = context;
                section.Add(new ReadoutElement(
                    () => capturedContext.GetStringValue("token_name"),
                    detail: () => TooltipReader.Lines(capturedRow)));
            }
            return root;
        }

        // A colour matching no group reads uncategorized, the same unexplained shade the
        // sighted player sees.
        private static string CategoryOf(Color colour) {
            string hex = "#" + ColorUtility.ToHtmlStringRGBA(colour);
            foreach (var (colorKey, labelKey) in Groups) {
                string groupHex = GameLoc.TryGet(colorKey);
                if (groupHex != null && string.Equals(groupHex.Trim(), hex, System.StringComparison.OrdinalIgnoreCase)) {
                    return GameLoc.TryGet(labelKey);
                }
            }
            return null;
        }
    }
}
