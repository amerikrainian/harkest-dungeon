using DD2A11y.Core.Input;
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

        /// <summary>Whether the screen's entry announcement may be spoken yet. A screen whose
        /// content binds a beat after it appears (a story's bark lines) answers false until the
        /// read would be complete; the router holds the name-then-landing announcement until
        /// then, capped so a screen that never settles still reads. Consulted only between
        /// entry and that first announcement.</summary>
        public virtual bool EntrySettled => true;

        /// <summary>Whether the current entry's name-then-landing announcement has been spoken.
        /// Owned by the router. A screen that holds its entry checks this to keep its own
        /// speech out of the announcement's words until they are out.</summary>
        public bool EntryAnnounced { get; internal set; }

        /// <summary>Whether the input gate takes the whole keyboard for this screen. An overlay
        /// that shares the keyboard with live gameplay (the road map: our arrows, the game's
        /// WASD) answers false and suppresses the specific game bindings it claims instead.</summary>
        public virtual bool CapturesKeyboard => true;

        private static readonly InputCategory[] UiOnlyCategories = { InputCategory.UI };

        /// <summary>The input categories live while this screen stands, highest-priority first
        /// (Global is always appended by the manager). A screen with commands of its own (the
        /// combat glances, the crossroads rename) declares their category here, so those keys
        /// are dead everywhere the commands do not apply.</summary>
        public virtual InputCategory[] InputCategories => UiOnlyCategories;

        /// <summary>First shot at a dispatched UI action, before the navigator's tree handling.
        /// A bespoke-cursor screen (the map viewer) consumes its keys here.</summary>
        public virtual bool HandleAction(string actionKey) => false;

        /// <summary>Called once when the router leaves this screen (it stopped matching or a
        /// higher screen took over) - the place to undo per-screen state (binding suppression)
        /// and to speak a dismissal where no underlying screen will re-announce.</summary>
        public virtual void OnLeave() { }
    }
}
