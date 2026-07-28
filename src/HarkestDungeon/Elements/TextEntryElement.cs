using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// A mod-owned free-text field any screen can host: Enter opens the mod's own typing mode
    /// (empty buffer), Enter commits the typed text - empty included, the host decides what that
    /// means - and Escape keeps the old value, re-reading the row. The host supplies the spoken
    /// value form (a separator setting reads spelled out, a name field reads whole), and an
    /// optional hint spoken after the edit-mode line: what a bare Enter will produce, spoken
    /// rather than prefilled so typing starts clean.
    /// </summary>
    public sealed class TextEntryElement : UIElement {
        private readonly Func<string> _label;
        private readonly Func<string> _value;
        private readonly Action<string> _onCommit;
        private readonly ModTextEdit _edit;
        private readonly Action<string, bool> _speak;
        private readonly Func<string> _hint;

        public TextEntryElement(Func<string> label, Func<string> value, Action<string> onCommit,
                                ModTextEdit edit, Action<string, bool> speak, Func<string> hint = null) {
            _label = label;
            _value = value;
            _onCommit = onCommit;
            _edit = edit;
            _speak = speak;
            _hint = hint;
        }

        public override string Label => _label();
        public override string Role => S.RoleEdit;
        public override string Value => _value();

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                if (_edit.Begin(Commit, Cancel) && _hint != null) {
                    _speak(_hint(), false);
                }
            });
        }

        // Queued, not interrupting: the host may have spoken its own commit feedback first
        // ("reset to default"), and nothing else is pending when an edit ends.
        private void Commit(string text) {
            _onCommit(text);
            _speak(_value(), false);
        }

        // Backed out: re-read the whole row, so the player hears where they stand.
        private void Cancel() => _speak(GetFocusText(), true);
    }
}
