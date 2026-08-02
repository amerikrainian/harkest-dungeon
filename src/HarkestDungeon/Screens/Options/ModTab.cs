using DD2A11y.Core.Nav;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// One mod-authored tab appended after the game's own tabs on the settings screen. The
    /// screen owns the tab selector and the shared item flow; a tab contributes its name and
    /// fills the flow with mod elements when it is the active one.
    /// </summary>
    public abstract class ModTab {
        /// <summary>The tab's spoken name on the tab selector.</summary>
        public abstract string Name { get; }

        /// <summary>Fill the screen's item flow with this tab's rows. Called on every switch to
        /// the tab, so rows are built fresh.</summary>
        public abstract void Populate(Container items);

        /// <summary>Called when the tab stops being the shown one (a switch away, the screen
        /// closing) - the place to end any per-tab state (a running sound preview).</summary>
        public virtual void OnHidden() { }
    }
}
