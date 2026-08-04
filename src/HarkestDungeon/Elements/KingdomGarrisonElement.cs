using System;
using System.Collections.Generic;
using Assets.Code.Kingdom.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Game;

namespace DD2A11y.Elements {
    /// <summary>
    /// One garrison slot on the kingdom inn panel - a stationed hero or a militia filler,
    /// labeled by the screen, with the widget's travel/immobile tooltips as the buffer. The
    /// grab key picks the hero up and places it on another slot (the keyboard face of the
    /// panel's reorder drag); the screen owns that flow via <see cref="Widget"/>.
    /// </summary>
    public sealed class KingdomGarrisonElement : UIElement {
        private readonly Func<string> _label;

        public KingdomGarrisonElement(KingdomInnPanelActorBhv widget, Func<string> label) {
            Widget = widget;
            _label = label;
        }

        public KingdomInnPanelActorBhv Widget { get; }

        public override bool CanFocus => Widget != null && Widget.gameObject.activeInHierarchy;

        public override string Label => _label();

        protected override IEnumerable<string> GetDetailLines() {
            if (Widget == null) {
                yield break;
            }
            foreach (var line in TooltipReader.Lines(Widget.gameObject)) {
                yield return line;
            }
        }
    }
}
