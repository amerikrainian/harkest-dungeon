using System;
using DD2A11y.Core.Nav;

namespace DD2A11y.Elements {
    /// <summary>A focusable line of read-only text (a modal's body, a disclaimer). Focusable so it
    /// sits in the Up/Down flow; it advertises no actions.</summary>
    public sealed class StaticTextElement : UIElement {
        private readonly Func<string> _text;

        public StaticTextElement(Func<string> text) {
            _text = text;
        }

        public override string Label => _text();
    }
}
