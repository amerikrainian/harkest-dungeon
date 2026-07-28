using System.Collections.Generic;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using HarmonyLib;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// The header row at the top of the patch notes: the current page's version heading, and
    /// the line source the screen reads its note rows from. Paging itself lives on the screen
    /// (Left/Right anywhere), so this element carries no actions.
    /// </summary>
    public sealed class PatchNotesPageElement : UIElement {
        private static readonly AccessTools.FieldRef<PatchNotesWidgetBhv, TextMeshProUGUI> TextField =
            AccessTools.FieldRefAccess<PatchNotesWidgetBhv, TextMeshProUGUI>("m_patchNotesText");

        private readonly PatchNotesWidgetBhv _widget;
        private readonly System.Func<bool> _ready;

        /// <param name="ready">Whether the widget has written the page it will settle on. Until
        /// then the label still holds the prefab's placeholder ("HOTFIX 0.13.{version}") or, on a
        /// reopen, the page left over from last time - neither worth speaking.</param>
        public PatchNotesPageElement(PatchNotesWidgetBhv widget, System.Func<bool> ready) {
            _widget = widget;
            _ready = ready;
        }

        public override bool CanFocus => _widget != null && _widget.gameObject.activeInHierarchy;

        public override string Value => Line(0);

        /// <summary>The page's line at <paramref name="index"/> (0 = the version header), or null
        /// past its end. Read live, so a row always speaks the page currently shown.</summary>
        public string Line(int index) {
            int i = 0;
            foreach (var line in SpokenLine.NonEmptyLines(PageText())) {
                if (i++ == index) {
                    return line;
                }
            }
            return null;
        }

        /// <summary>The raw page text, for the screen's rebuild check. Null until the widget has
        /// written the page it settles on.</summary>
        public string PageText() {
            if (!_ready()) {
                return null;
            }
            var text = TextField(_widget);
            return text == null ? null : text.text;
        }

        // No adjust actions: paging is the screen's, live from anywhere on it.
        public override IEnumerable<ElementAction> GetActions() {
            yield break;
        }
    }
}
