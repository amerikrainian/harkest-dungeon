using System;

namespace DD2A11y.Core.Nav {
    /// <summary>
    /// A transient option menu an element opens over the current screen (a dropdown's choices).
    /// The navigator owns its lifecycle: while open it is the whole navigable tree, activating an
    /// item commits and closes it, Escape closes it, and a screen change closes it too.
    /// <see cref="OnClosed"/> runs on every close path, so the element can tear down whatever
    /// game-side view the popup mirrors.
    /// </summary>
    public sealed class Popup {
        public Container Root { get; }
        public Action? OnClosed { get; }

        public Popup(Container root, Action? onClosed = null) {
            Root = root;
            OnClosed = onClosed;
        }
    }
}
