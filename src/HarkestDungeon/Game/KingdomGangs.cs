namespace DD2A11y.Game {
    /// <summary>The spoken name of a Kingdoms enemy gang, by the game's own campaign title for
    /// it ("Secrets of the Coven", "Hunger of the Beast Clan") - the only localized text the
    /// game attaches to a gang; its banner and flag are images.</summary>
    internal static class KingdomGangs {
        /// <summary>The title for a gang id; some ids reach here pluralized (a coach skin's
        /// "beastmen"), so the singular is retried, and an id titled nowhere speaks its own
        /// words.</summary>
        internal static string Title(string id) {
            if (string.IsNullOrEmpty(id)) {
                return null;
            }
            return GameLoc.TryGet("kingdom_select_gang_disclaimer_title_" + id)
                ?? (id.EndsWith("s", System.StringComparison.Ordinal)
                    ? GameLoc.TryGet("kingdom_select_gang_disclaimer_title_" + id.Substring(0, id.Length - 1)) : null)
                ?? id.Replace('_', ' ');
        }
    }
}
