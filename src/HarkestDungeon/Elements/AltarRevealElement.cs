using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;

namespace DD2A11y.Elements {
    /// <summary>
    /// The altar reveal modal's text: the just-unlocked item's name and description, spoken
    /// in full on entry and reviewable line by line in the buffer. Enter continues past the
    /// reveal - the game's own Submit step.
    /// </summary>
    public sealed class AltarRevealElement : UIElement {
        private readonly Action _resume;
        private readonly Func<string> _name;
        private readonly Func<string> _description;

        public AltarRevealElement(Action resume, Func<string> name, Func<string> description) {
            _resume = resume;
            _name = name;
            _description = description;
        }

        public override string Label => _name();

        public override string GetFocusText() {
            var parts = new List<string> { _name() };
            parts.AddRange(DescriptionLines());
            return SpokenLine.Join(", ", parts);
        }

        protected override IEnumerable<string> GetDetailLines() => DescriptionLines();

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _resume);
        }

        private IEnumerable<string> DescriptionLines() {
            string description = _description();
            if (string.IsNullOrEmpty(description)) {
                yield break;
            }
            foreach (var line in TextFilter.Clean(description).Split('\n')) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    yield return line;
                }
            }
        }
    }
}
