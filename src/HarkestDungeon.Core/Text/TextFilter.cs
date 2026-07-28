using System.Text.RegularExpressions;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Text {
    /// <summary>
    /// Normalizes raw game text for speech. DD2 labels are TextMeshPro, so they carry rich-text
    /// markup (&lt;color&gt;, &lt;b&gt;, &lt;sprite&gt;, ...) and hard line breaks. Strip the markup,
    /// turn line breaks into sentence breaks so multi-line text reads with a pause, and collapse the
    /// remaining whitespace. Pure and unit-tested.
    /// </summary>
    public static class TextFilter {
        private static readonly Regex RichTags = new Regex("<[^>]+>", RegexOptions.Compiled);
        // A line break (with surrounding whitespace) after text that does not already end a sentence.
        private static readonly Regex BreakAfterText = new Regex("(?<=[^\\s.!?,:;])\\s*[\\r\\n]+\\s*", RegexOptions.Compiled);
        // Any remaining line break (after sentence punctuation, where the pause already exists).
        private static readonly Regex LineBreak = new Regex("\\s*[\\r\\n]+\\s*", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.Compiled);
        // A sentence period and a separating comma that collide when game text (which usually ends a
        // sentence with a period) is concatenated with our own delimiter (a comma). The second mark is
        // the deliberate one, so it wins: ".," reads as "," and ",." reads as ".". Mixed pairs only, so
        // an ellipsis (a run of identical dots) is never touched.
        private static readonly Regex PeriodThenComma = new Regex("\\.\\s*,", RegexOptions.Compiled);
        private static readonly Regex CommaThenPeriod = new Regex(",\\s*\\.", RegexOptions.Compiled);
        // Whitespace stranded before a punctuation mark - typically a zero-width space TMP
        // injected at a word edge (folded to a real space above) colliding with our ", " joiner.
        private static readonly Regex SpaceBeforeMark = new Regex("\\s+([,.:;!?])", RegexOptions.Compiled);
        // Unicode bidi control characters. They shape visual text direction, which speech has none of,
        // and a synthesizer fed one may announce or garble it.
        private static readonly Regex BidiControls =
            new Regex("[\\u061C\\u200E\\u200F\\u202A-\\u202E\\u2066-\\u2069]", RegexOptions.Compiled);
        // The game's "???" placeholder glyph (a locked confession's name, an unexplored node's
        // "Rewards: ???"): meaning-bearing, but a synthesizer voices bare question marks as
        // nothing, so a free-standing run becomes a word. Runs attached to a word ("What???"
        // in a bark) keep their marks.
        private static readonly Regex UnknownGlyph =
            new Regex("(?<![\\w?])\\?{2,}(?![\\w?])", RegexOptions.Compiled);

        public static string Clean(string? raw) {
            if (string.IsNullOrEmpty(raw)) {
                return string.Empty;
            }

            // Meaning-bearing sprite glyphs become words before the markup strip discards them.
            string s = SpriteText.Expand(raw!);
            s = RichTags.Replace(s, string.Empty);
            s = s.Replace(' ', ' ');   // non-breaking space
            s = s.Replace('​', ' ');   // zero-width space TMP sometimes injects
            s = BidiControls.Replace(s, string.Empty);
            s = UnknownGlyph.Replace(s, S.TextUnknown);
            s = FoldPunctuation(s);
            s = s.Trim();
            // A line break in multi-line game text (e.g. a tooltip listing effects on separate lines)
            // becomes a sentence break so it reads with a pause. When the line already ends with
            // sentence punctuation the break is just a space, so the punctuation is not doubled.
            s = BreakAfterText.Replace(s, ". ");
            s = LineBreak.Replace(s, " ");
            // Collapse a period-then-comma (or comma-then-period) collision to the second, deliberate
            // mark before the whitespace pass tidies the spacing it leaves behind.
            s = PeriodThenComma.Replace(s, ", ");
            s = CommaThenPeriod.Replace(s, ". ");
            s = Whitespace.Replace(s, " ").Trim();
            s = SpaceBeforeMark.Replace(s, "$1");
            return s;
        }

        // Fold the Unicode typographic punctuation common in game text (smart dashes, curly quotes,
        // ellipsis) to plain ASCII so it reads cleanly - an em dash in particular is otherwise
        // announced as "dash" and breaks the flow.
        private static string FoldPunctuation(string s) {
            s = s.Replace('–', '-')   // en dash
                 .Replace('—', '-')   // em dash
                 .Replace('―', '-')   // horizontal bar
                 .Replace('‒', '-')   // figure dash
                 .Replace('−', '-')   // minus sign
                 .Replace('‘', '\'')  // left single quote
                 .Replace('’', '\'')  // right single quote / apostrophe
                 .Replace('“', '"')   // left double quote
                 .Replace('”', '"');  // right double quote
            return s.Replace("…", "...");  // ellipsis
        }
    }
}
