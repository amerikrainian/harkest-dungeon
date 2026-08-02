using System;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A treatable quirk row at the hospital: the quirk's own name, "selected" while it is
    /// the one the lock/remove commands would treat. Enter is the row's own click (the
    /// game's selection), spoken back as the landed state; the quirk's description rides
    /// the row's tooltip into the buffer.
    /// </summary>
    public sealed class HospitalQuirkElement : SelectableElement {
        private readonly Func<bool> _selected;

        public HospitalQuirkElement(Button button, Func<bool> selected) : base(button) {
            _selected = selected;
        }

        public override string Status => _selected() ? S.StatusSelected : null;

        public override bool ReannounceOnActivate => true;

        public override string GetValueText() => SpokenLine.Join(Status, Label);
    }
}
