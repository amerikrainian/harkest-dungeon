using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;

namespace DD2A11y.Elements {
    /// <summary>A screen root that can advertise the screen-level back action (Escape).</summary>
    public sealed class RootContainer : Container {
        private readonly Action _back;

        public RootContainer(ContainerShape shape = ContainerShape.VerticalList, Action back = null)
            : base(shape) {
            _back = back;
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (_back != null) {
                yield return new ElementAction(ActionIds.Back, _back);
            }
        }
    }
}
