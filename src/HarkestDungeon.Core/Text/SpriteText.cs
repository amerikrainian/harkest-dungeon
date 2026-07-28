using System;
using System.Text.RegularExpressions;

namespace DD2A11y.Core.Text {
    /// <summary>
    /// Expands TMP inline sprite tags into words before markup stripping. Game text embeds
    /// meaning-bearing glyphs (&lt;sprite name="token_combo"&gt; is the combo token, an
    /// icon_healthup marks a heal); stripping them as markup would silently drop information.
    /// The engine-side resolver maps a sprite name to its word (the game's own token name, an
    /// authored word for the small icon set); a sprite it cannot name is left for the markup
    /// strip, matching the old behavior.
    /// </summary>
    public static class SpriteText {
        private static readonly Regex SpriteTag =
            new Regex("<sprite[^>]*?\\bname=\"([^\"]+)\"[^>]*>", RegexOptions.Compiled);

        /// <summary>Maps a sprite name to a spoken word, or null to drop the sprite. Set once by
        /// the plugin at load; null in unit tests that exercise the raw filter.</summary>
        public static Func<string, string?>? Resolver { get; set; }

        public static string Expand(string raw) {
            var resolver = Resolver;
            if (resolver == null || raw.IndexOf("<sprite", StringComparison.Ordinal) < 0) {
                return raw;
            }
            return SpriteTag.Replace(raw, match => {
                string? word = resolver(match.Groups[1].Value);
                // Spaces keep the word from gluing onto adjacent text ("heal 10%", not "heal10%");
                // the whitespace collapse later tidies any doubling.
                return word == null ? match.Value : " " + word + " ";
            });
        }
    }
}
