using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// The tab header of a tabbed screen, sitting first in the screen's vertical flow: Left/Right
    /// switch tabs (via the advertised increase/decrease actions the navigator's adjust path
    /// drives), Down walks into the active tab's items. Speaks the landed tab as "name, tab".
    /// The screen owns rebuilding the items below on a switch.
    /// </summary>
    public sealed class TabSelectorElement : UIElement {
        private readonly Func<int> _current;
        private readonly Func<int> _count;
        private readonly Func<int, string> _nameAt;
        private readonly Action<int> _select;

        public TabSelectorElement(Func<int> current, Func<int> count, Func<int, string> nameAt, Action<int> select) {
            _current = current;
            _count = count;
            _nameAt = nameAt;
            _select = select;
        }

        public override string Label => _nameAt(_current());
        public override string Role => S.RoleTab;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Increase, () => Step(+1));
            yield return new ElementAction(ActionIds.Decrease, () => Step(-1));
        }

        private void Step(int direction) {
            int next = _current() + direction;
            if (next >= 0 && next < _count()) {
                _select(next);
            }
        }

        // A switch reads the landed tab in full ("Graphics, tab"); a clamped adjust at either end
        // re-reads the current tab rather than saying minimum/maximum.
        public override string GetAdjustText(string actionId, bool changed)
            => SpokenLine.Join(Label, Role);
    }
}
