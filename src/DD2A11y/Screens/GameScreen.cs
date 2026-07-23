using DD2A11y.Core.Nav;

namespace DD2A11y.Screens {
    /// <summary>
    /// Base for a navigable screen: it matches a live game surface, carries an authored name
    /// spoken on entry, and builds its element tree fresh each time it is entered (read live,
    /// never cached). The ScreenRouter resolves the active screen once per frame in registration
    /// order and attaches the navigator to the built root.
    /// </summary>
    public abstract class GameScreen {
        /// <summary>Authored screen name spoken when the screen is entered.</summary>
        public abstract string Name { get; }

        /// <summary>The live game object/component this screen would read right now, or null when
        /// it does not apply. A CHANGED token (reference inequality) re-enters the screen, so
        /// return the stable per-instance component, not a per-frame temporary.</summary>
        public abstract object ResolveTarget();

        /// <summary>Build the navigable tree from live game state. Called on each entry.</summary>
        public abstract Container BuildRoot(object target);

        /// <summary>Called every frame while this screen stands. A dynamic screen rebuilds its
        /// sub-trees here when the game state they mirror changed. It must NOT announce - it
        /// returns true to request one, and the router owns the announce (alongside the
        /// navigator's focus-validity check) so the read is single and reflects the rebuilt
        /// tree.</summary>
        public virtual bool OnUpdate(object target) => false;
    }
}
