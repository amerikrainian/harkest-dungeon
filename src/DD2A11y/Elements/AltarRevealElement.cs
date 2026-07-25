using System;
using System.Collections.Generic;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;

namespace DD2A11y.Elements {
    /// <summary>
    /// The altar reveal modal's text: the just-unlocked item's name and description, spoken
    /// in full on entry and reviewable line by line in the buffer. Enter continues past the
    /// reveal - the game's own Submit step.
    /// </summary>
    public sealed class AltarRevealElement : UIElement {
        private readonly AltarItemSubScreenBhv _panel;
        private readonly Func<string> _name;
        private readonly Func<string> _description;

        public AltarRevealElement(AltarItemSubScreenBhv panel, Func<string> name, Func<string> description) {
            _panel = panel;
            _name = name;
            _description = description;
        }

        public override string Label => _name();

        public override string GetFocusText() {
            var parts = new List<string> { _name() };
            parts.AddRange(DescriptionLines());
            return SpokenLine.Join(", ", parts);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return _name();
            foreach (var line in DescriptionLines()) {
                yield return line;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => _panel.OnTimelineResume());
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
