using System.Collections.Generic;
using Assets.Code.Library;
using Assets.Code.Token;
using Assets.Code.Utils;
using DD2A11y.Core.Text;

namespace DD2A11y.Game {
    /// <summary>
    /// The mouse-over half of token glyphs: any buffer whose lines carry token sprites gets one
    /// trailing line per unique token - the game's own tooltip composition ("Block: Takes -50%
    /// DMG from next hit"), what a sighted player hovers the icon for. A token whose description
    /// already appears among the lines (a combatant's token rows) is not repeated. Wired into
    /// <see cref="Core.Nav.UIElement.BufferGlossary"/> at startup.
    /// </summary>
    public static class TokenGlossary {
        private const string SpritePrefix = "token_";

        public static IEnumerable<string> Lines(IReadOnlyList<string> bufferLines, string context) {
            var ids = new List<string>();
            foreach (var line in bufferLines) {
                foreach (var sprite in SpriteText.Names(line)) {
                    if (!sprite.StartsWith(SpritePrefix, System.StringComparison.Ordinal)) {
                        continue;
                    }
                    string id = WithVariant(TokenIdOf(sprite), context);
                    if (id != null && !ids.Contains(id)) {
                        ids.Add(id);
                    }
                }
            }
            foreach (var id in ids) {
                string description = DescriptionOf(id);
                if (description == null || AlreadyPresent(bufferLines, description)) {
                    continue;
                }
                yield return Compose(id, description);
            }
        }

        // A hero path swaps a token for a suffixed variant with its own description (the
        // Duelist's stances: "dul_defensive_stance_p2" on her second path) while the skill
        // text keeps the base glyph. The describing surface's own id carries the same suffix
        // ("dul_disengage_p2"), so a glyph resolves to the variant that surface concerns.
        private static string WithVariant(string id, string context) {
            if (id == null || context == null) {
                return id;
            }
            var suffix = System.Text.RegularExpressions.Regex.Match(context, "_p[0-9]+");
            if (!suffix.Success) {
                return id;
            }
            string variant = id + suffix.Value;
            return SingletonMonoBehaviour<Library<string, TokenDefinition>>.Instance
                .GetLibraryElement(variant) != null ? variant : id;
        }

        /// <summary>The token id behind a glyph's sprite name. Usually the sprite is
        /// "token_&lt;id&gt;", but some icons diverge from their token ("token_blind-line" is
        /// blind's icon), so the fallback finds the token whose own glyph loc entry names this
        /// sprite - the same data the game renders the icon from.</summary>
        public static string TokenIdOf(string spriteName) {
            var library = SingletonMonoBehaviour<Library<string, TokenDefinition>>.Instance;
            if (library == null) {
                return null;
            }
            string bare = spriteName.Substring(SpritePrefix.Length);
            if (library.GetLibraryElement(bare) != null) {
                return bare;
            }
            string marker = "\"" + spriteName + "\"";
            foreach (var definition in library.GetLibraryElements()) {
                string glyph = GameLoc.TryGet(SpritePrefix + definition.m_Id);
                if (glyph != null && glyph.Contains(marker)) {
                    return definition.m_Id;
                }
            }
            return null;
        }

        // A guard token's plain description is a "{0} guards" format; the game's tooltip uses
        // the unsourced variant when no guardian is named, and so does this.
        private static string DescriptionOf(string id) {
            var definition = SingletonMonoBehaviour<Library<string, TokenDefinition>>.Instance
                .GetLibraryElement(id);
            string key = definition != null && definition.GetHasType(TokenType.GUARD)
                ? SpritePrefix + id + "_description_unsourced"
                : SpritePrefix + id + "_description";
            return GameLoc.TryGet(key);
        }

        private static bool AlreadyPresent(IReadOnlyList<string> lines, string description) {
            string clean = TextFilter.Clean(description);
            if (clean.Length == 0) {
                return true;
            }
            foreach (var line in lines) {
                if (TextFilter.Clean(line).Contains(clean)) {
                    return true;
                }
            }
            return false;
        }

        // The game's own name-colon-description tooltip line ("token_tooltip_format").
        private static string Compose(string id, string description) {
            string name = GameLoc.TryGet("token_name_" + id) ?? GameLoc.TryGet(SpritePrefix + id);
            string format = GameLoc.TryGet("token_tooltip_format");
            return format == null
                ? name + ": " + description
                : string.Format(format, string.Empty, name, description);
        }
    }
}
