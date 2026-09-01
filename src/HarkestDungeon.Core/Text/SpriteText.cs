using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DD2A11y.Core.Text {
    /// <summary>
    /// Expands TMP inline sprite tags into words before markup stripping. Game text embeds
    /// meaning-bearing glyphs (&lt;sprite name="token_combo"&gt; is the combo token, an
    /// icon_healthup marks a heal); stripping them as markup would silently drop information.
    /// The engine-side resolver maps a sprite name to its word (the game's own token name, an
    /// authored word for the small icon set); a sprite it cannot name is left for the markup
    /// strip, matching the old behavior. A glyph captioned by its own word in the text
    /// ("Baubles [icon] are used", "+2 [icon] Mastery") is decorative there and dropped, so the
    /// word is spoken once.
    /// </summary>
    public static class SpriteText {
        private static readonly Regex SpriteTag =
            new Regex("<sprite[^>]*?\\bname=\"([^\"]+)\"[^>]*>", RegexOptions.Compiled);
        private static readonly Regex RichTags = new Regex("<[^>]+>", RegexOptions.Compiled);
        // Whitespace and the brackets a caption sits in ("repair [icon] (armor)").
        private static readonly char[] CaptionGap = { ' ', '\t', '\r', '\n', ' ', '(', ')', '[', ']' };

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
                if (word == null) {
                    return match.Value;
                }
                // Spaces keep the word from gluing onto adjacent text ("heal 10%", not "heal10%");
                // the whitespace collapse later tidies any doubling.
                return IsCaptioned(raw, match, word) ? " " : " " + word + " ";
            });
        }

        // The text already carries the glyph's word right beside it: ending the text before the
        // glyph, alone or as the head of a two-word compound ("Mastery points [icon]"), or
        // starting the text after it. Markup between them is ignored. The window after the glyph
        // is one word only: in "Quirks with [rare icon] are rare" the word two ahead is the
        // sentence's own use, and the glyph is the subject.
        private static bool IsCaptioned(string raw, Match match, string word) {
            string before = RichTags.Replace(raw.Substring(0, match.Index), string.Empty)
                .TrimEnd(CaptionGap);
            if (EndsWithWord(before, word) || EndsWithWord(TrimLastWord(before), word)) {
                return true;
            }
            string after = RichTags.Replace(raw.Substring(match.Index + match.Length), string.Empty)
                .TrimStart(CaptionGap);
            return StartsWithWord(after, word);
        }

        private static bool EndsWithWord(string text, string word)
            => text.EndsWith(word, StringComparison.OrdinalIgnoreCase)
               && (text.Length == word.Length || !char.IsLetterOrDigit(text[text.Length - word.Length - 1]));

        private static bool StartsWithWord(string text, string word)
            => text.StartsWith(word, StringComparison.OrdinalIgnoreCase)
               && (text.Length == word.Length || !char.IsLetterOrDigit(text[word.Length]));

        // "Mastery points" -> "Mastery": the trailing word and the whitespace before it.
        private static string TrimLastWord(string text) {
            int end = text.Length;
            while (end > 0 && char.IsLetterOrDigit(text[end - 1])) {
                end--;
            }
            return end == text.Length ? string.Empty : text.Substring(0, end).TrimEnd(CaptionGap);
        }

        /// <summary>Removes sprite tags outright, leaving whatever plain text stands between
        /// them.</summary>
        public static string Strip(string raw) {
            if (raw.IndexOf("<sprite", StringComparison.Ordinal) < 0) {
                return raw;
            }
            return SpriteTag.Replace(raw, string.Empty);
        }

        /// <summary>The sprite names embedded in raw text, in order of appearance.</summary>
        public static IEnumerable<string> Names(string? raw) {
            if (raw == null || raw.IndexOf("<sprite", StringComparison.Ordinal) < 0) {
                yield break;
            }
            foreach (Match match in SpriteTag.Matches(raw)) {
                yield return match.Groups[1].Value;
            }
        }
    }
}
