using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>The battle header's turn-order readout. Focus speaks the whole remaining order
    /// as one line; the buffer carries just one combatant name per line so review steps the
    /// order name by name. The names read live at speech time through the provided delegate,
    /// current actor first.</summary>
    public sealed class TurnOrderElement : UIElement {
        private readonly Func<IReadOnlyList<string>> _names;

        public TurnOrderElement(Func<IReadOnlyList<string>> names) {
            _names = names;
        }

        public override bool CanFocus => _names().Count > 0;

        public override string Label {
            get {
                var names = _names();
                return names.Count == 0 ? null : S.CombatTurnOrder(SpokenLine.Join(SpokenLine.Separator, names));
            }
        }

        public override string GetBufferHeadText() {
            var names = _names();
            return names.Count == 0 ? null : names[0];
        }

        protected override IEnumerable<string> GetBufferHeadParts() {
            yield return GetBufferHeadText();
        }

        protected override IEnumerable<string> GetDetailLines() {
            var names = _names();
            for (int i = 1; i < names.Count; i++) {
                yield return names[i];
            }
        }
    }
}
