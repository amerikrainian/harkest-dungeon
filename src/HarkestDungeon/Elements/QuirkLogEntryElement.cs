using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Run.RunLogEntry;
using Assets.Code.UI.RunLog;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// A travelogue quirk entry (<c>QuirkLogEntryBhv</c>): the game keeps the quirk's name
    /// behind a mask until the row is clicked, showing only its "X gained a Quirk!" title and
    /// a "Click to Reveal" hint. Unrevealed, the row reads that title as a button with the
    /// hint in the buffer and Enter is the row's own reveal; revealed, it reads the game's
    /// bound line - who gained what, and the source when the log names one - with the
    /// quirk's own tooltip (its effects) in the buffer.
    /// </summary>
    public sealed class QuirkLogEntryElement : UIElement {
        private static readonly AccessTools.FieldRef<QuirkLogEntryBhv, QuirkLogEntry> EntryField =
            AccessTools.FieldRefAccess<QuirkLogEntryBhv, QuirkLogEntry>("m_quirkLogEntry");

        private readonly QuirkLogEntryBhv _row;

        public QuirkLogEntryElement(QuirkLogEntryBhv row) {
            _row = row;
        }

        private bool Revealed {
            get {
                var entry = _row == null ? null : EntryField(_row);
                return entry != null && entry.Revealed;
            }
        }

        public override bool CanFocus => _row != null && _row.gameObject.activeInHierarchy;

        public override string Label {
            get {
                if (_row == null) {
                    return null;
                }
                if (!Revealed) {
                    return UiText.FirstLabel(_row.gameObject);
                }
                var context = _row.GetComponent<DataContextBhv>();
                if (context == null) {
                    return UiText.AllText(_row.gameObject);
                }
                string gained = SpokenLine.Join(" ", new[] {
                    context.GetStringValue("gained_label"), context.GetStringValue("quirk_label") });
                return SpokenLine.Join(gained, context.GetStringValue("source_label"));
            }
        }

        public override string Role => Revealed ? null : S.RoleButton;

        // The reveal marks the entry at once (the timeline only unmasks the art), so the
        // revealed line speaks as the press's own answer.
        public override IEnumerable<ElementAction> GetActions() {
            if (!Revealed) {
                yield return new ElementAction(ActionIds.Activate, () => {
                    _row.OnClick();
                    Core.Speech.SpeechPipeline.Instance?.Speak(GetFocusText(), interrupt: true);
                });
            }
        }

        protected override IEnumerable<string> GetDetailLines()
            => _row == null ? System.Linq.Enumerable.Empty<string>() : TooltipReader.Lines(_row.gameObject);
    }
}
