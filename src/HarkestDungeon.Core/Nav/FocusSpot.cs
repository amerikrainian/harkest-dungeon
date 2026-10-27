using System.Collections.Generic;

namespace DD2A11y.Core.Nav {
    /// <summary>
    /// Where focus sits within content a screen rebuilds under it (a hero switch, a return
    /// from a screen pushed above): the scope's child on the focus path, by label and by
    /// position, then the position within that child. Resolved against the rebuilt content it
    /// finds the same spot - the child carrying the same label, else the one at the same
    /// position, clamped - so focus stays put across content that differs only in its values.
    /// </summary>
    public sealed class FocusSpot {
        private readonly string? _label;
        private readonly int _index;
        private readonly int _inner;

        private FocusSpot(string? label, int index, int inner) {
            _label = label;
            _index = index;
            _inner = inner;
        }

        /// <summary>The spot of the focus path within <paramref name="scope"/>, or null when
        /// focus is outside it.</summary>
        public static FocusSpot? Capture(Container scope, IReadOnlyList<UIElement> path) {
            for (int i = 0; i < path.Count; i++) {
                if (path[i].Parent != scope) {
                    continue;
                }
                var top = path[i];
                int inner = top is Container row && i + 1 < path.Count ? IndexOf(row, path[i + 1]) : -1;
                return new FocusSpot(top.Label, IndexOf(scope, top), inner);
            }
            return null;
        }

        /// <summary>The leaf to land on in the rebuilt <paramref name="scope"/>, or null when it
        /// holds nothing focusable.</summary>
        public UIElement? Resolve(Container scope) {
            UIElement? top = null;
            if (_label != null) {
                foreach (var child in scope.Children) {
                    if (child.Label == _label && Landable(child)) {
                        top = child;
                        break;
                    }
                }
            }
            top ??= Nearest(scope, _index);
            if (top is Container row) {
                return Descend((_inner >= 0 ? Nearest(row, _inner) : null) ?? row.FirstFocusable());
            }
            return top;
        }

        // The landable child at the index, else the closest one before it, else after it.
        private static UIElement? Nearest(Container container, int index) {
            var children = container.Children;
            int start = index < children.Count ? index : children.Count - 1;
            for (int i = start; i >= 0; i--) {
                if (Landable(children[i])) {
                    return children[i];
                }
            }
            for (int i = start + 1; i < children.Count; i++) {
                if (Landable(children[i])) {
                    return children[i];
                }
            }
            return null;
        }

        private static UIElement? Descend(UIElement? element) {
            while (element is Container container) {
                element = container.FirstFocusable();
            }
            return element;
        }

        private static bool Landable(UIElement element)
            => element.CanFocus && !(element is Container container && container.IsEmptyContainer);

        private static int IndexOf(Container container, UIElement element) {
            var children = container.Children;
            for (int i = 0; i < children.Count; i++) {
                if (children[i] == element) {
                    return i;
                }
            }
            return -1;
        }
    }
}
