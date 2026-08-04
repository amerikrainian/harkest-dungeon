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
        private readonly Func<string> _value;
        private readonly Func<string> _status;

        public ActionElement(Func<string> label, string role, Action activate,
                             Func<IEnumerable<string>> extraBufferLines = null,
                             Func<string> value = null, Func<string> status = null) {
            _label = label;
            _role = role;
            _activate = activate;
            _extraBufferLines = extraBufferLines;
            _value = value;
            _status = status;
        }

        public override string Status => _status?.Invoke();
        public override string Label => _label();
        public override string Role => _role;
        public override string Value => _value?.Invoke();

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _activate);
        }

        protected override IEnumerable<string> GetDetailLines() {
            if (_extraBufferLines == null) {
                yield break;
            }
            foreach (var line in _extraBufferLines()) {
                yield return line;
            }
        }
    }
}
