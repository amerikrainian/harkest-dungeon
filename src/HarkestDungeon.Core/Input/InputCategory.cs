namespace DD2A11y.Core.Input {
    /// <summary>
    /// The input layer an action belongs to. Each frame the manager builds the set of LIVE categories
    /// from the active screen (via <see cref="InputManager.ActiveCategoriesProvider"/>, in priority
    /// order) plus <see cref="Global"/>, which is always on. Within the live set, an identical chord
    /// bound in two categories resolves to the higher-priority (earlier) one.
    /// </summary>
    public enum InputCategory {
        /// <summary>Always live, even with no screen captured (global hotkeys).</summary>
        Global,

        /// <summary>Screen/menu navigation and buffer review. Live only while a screen owns the
        /// keyboard; nav keys are routed into the active navigator rather than firing a handler
        /// directly.</summary>
        UI,

        /// <summary>Battlefield keys (combatant glances, the inspector), declared by the combat
        /// floor and its inspector overlay. Declared ahead of UI, so a chord both categories
        /// carry resolves to the combat command there.</summary>
        Combat,

        /// <summary>Hero-roster keys (rename, name reroll), declared by the screens whose hero
        /// slots advertise those actions (the crossroads).</summary>
        Roster,

        /// <summary>Road-story keys (the focused choice's hero vitals), declared by the story
        /// screen.</summary>
        Story,
    }
}
