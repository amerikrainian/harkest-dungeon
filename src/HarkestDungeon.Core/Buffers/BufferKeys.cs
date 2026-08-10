namespace DD2A11y.Core.Buffers {
    /// <summary>
    /// The registered buffer keys, in cycling order. The ui buffer carries the focused element's
    /// own composition; the side buffers (upgrade, hero) are filled by the focused element
    /// through <see cref="Nav.UIElement.GetSideBufferLines"/>; the battlefield buffers (enemies,
    /// party) and the combat log read screen-wide combat state and are empty outside a battle;
    /// the subtitles buffer holds the on-screen subtitle history while the game's subtitles
    /// setting is on.
    /// </summary>
    public static class BufferKeys {
        public const string Ui = "ui";
        public const string Upgrade = "upgrade";
        public const string Hero = "hero";
        public const string Enemies = "enemies";
        public const string Party = "party";
        public const string Combat = "combat";
        public const string Subtitles = "subtitles";
    }
}
