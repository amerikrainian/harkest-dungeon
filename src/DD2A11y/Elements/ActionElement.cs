using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;

namespace DD2A11y.Elements {
    /// <summary>A labeled command that drives a game method directly (a dialog's confirm, the
    /// disclaimer's continue). Label and buffer lines read live.</summary>
    public sealed class ActionElement : UIElement {
        private readonly Func<string> _label;
        private readonly string _role;
        private readonly Action _activate;
        private readonly Func<IEnumerable<string>> _extraBufferLines;

        public ActionElement(Func<string> label, string role, Action activate,
                             Func<IEnumerable<string>> extraBufferLines = null) {
            _label = label;
            _role = role;
            _activate = activate;
            _extraBufferLines = extraBufferLines;
        }

        public override string Label => _label();
        public override string Role => _role;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _activate);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            if (_extraBufferLines == null) {
                yield break;
            }
            foreach (var line in _extraBufferLines()) {
                yield return line;
            }
        }
    }
}
