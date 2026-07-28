using System.Collections.Generic;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using HarmonyLib;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// The patch notes pages as one control: the focus line is the current page's header line
    /// (the version heading), Left/Right flip pages through the widget's own methods (newest
    /// page first, so Right goes further back), and the buffer carries the whole page a line at
    /// a time. Both flip actions are always advertised - the widget refuses at the ends and the
    /// unchanged header reads back as "minimum"/"maximum".
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

        public override string Value {
            get {
                foreach (var line in SpokenLine.NonEmptyLines(PageText())) {
                    return line;
                }
                return null;
            }
        }

        private string PageText() {
            if (!_ready()) {
                return null;
            }
            var text = TextField(_widget);
            return text == null ? null : text.text;
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Increase, _widget.TryNextPage);
            yield return new ElementAction(ActionIds.Decrease, _widget.TryPreviousPage);
        }

        public override IEnumerable<string> GetBufferLines() {
            foreach (var line in SpokenLine.NonEmptyLines(PageText())) {
                yield return line;
            }
        }
    }
}
