using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;

namespace DD2A11y.Elements {
    /// <summary>A focusable read-only stat or fact (a resistance row, a quirk): terse label and
    /// value on focus, with the fuller detail (a tooltip breakdown, a description) as buffer
    /// lines. All three read live at speech time through the provided delegates.</summary>
    public sealed class ReadoutElement : UIElement {
        private readonly Func<string> _label;
        private readonly Func<string> _value;
        private readonly Func<IEnumerable<string>> _detail;

        public ReadoutElement(Func<string> label, Func<string> value = null, Func<IEnumerable<string>> detail = null) {
            _label = label;
            _value = value;
            _detail = detail;
        }

        public override bool CanFocus => !string.IsNullOrEmpty(Label);

        public override string Label => _label();

        public override string Value => _value?.Invoke();

        protected override IEnumerable<string> GetDetailLines() {
            if (_detail == null) {
                yield break;
            }
            foreach (var line in _detail()) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    yield return line;
                }
            }
        }
    }
}
