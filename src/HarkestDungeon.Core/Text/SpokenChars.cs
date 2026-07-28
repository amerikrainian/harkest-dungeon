using System.Text;

namespace DD2A11y.Core.Text {
    /// <summary>Characters as spoken tokens, for typing echo and for reading back text whose
    /// characters ARE the content (a separator setting): a space is named (a bare space is
    /// inaudible), everything else is the character itself - the screen reader applies the
    /// user's own punctuation level to it.</summary>
    public static class SpokenChars {
        public static string Name(char c) => c == ' ' ? Strings.Strings.EditSpace : c.ToString();

        /// <summary>The text character by character, tokens space-joined ("comma space").</summary>
        public static string Spell(string text) {
            var sb = new StringBuilder();
            foreach (char c in text) {
                if (sb.Length > 0) {
                    sb.Append(' ');
                }
                sb.Append(Name(c));
            }
            return sb.ToString();
        }
    }
}
