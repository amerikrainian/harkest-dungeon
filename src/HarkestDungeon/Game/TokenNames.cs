namespace DD2A11y.Game {
    /// <summary>The spoken name of a token: the game's glyphed token string ("token_&lt;id&gt;",
    /// expanded to words by the sprite pipeline), or its plain name ("token_name_&lt;id&gt;") for
    /// tokens that define only that - the Violinist's song-part markers carry no glyphed entry,
    /// and the game's GetNameString would hand the raw id to speech. Null when the game names
    /// the token nowhere.</summary>
    public static class TokenNames {
        public static string Spoken(string tokenId) {
            string name = GameLoc.TryGet("token_" + tokenId) ?? GameLoc.TryGet("token_name_" + tokenId);
            if (name == null) {
                Plugin.Log.LogWarning($"TokenNames: token '{tokenId}' has no name string");
            }
            return name;
        }
    }
}
