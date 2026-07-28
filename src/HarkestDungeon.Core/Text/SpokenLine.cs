using System.Collections.Generic;
using System.Text;

namespace DD2A11y.Core.Text {
    /// <summary>Joins the non-empty parts of one spoken line.</summary>
    public static class SpokenLine {
        /// <summary>Non-empty parts joined with ", " (the standard announcement separator).</summary>
        public static string Join(params string?[] parts) => Join(", ", parts);

        public static string Join(string separator, IEnumerable<string?> parts) {
            var sb = new StringBuilder();
            foreach (string? part in parts) {
                // Game strings sometimes carry stray edge whitespace ("Pass "); trimmed here so
                // the separator lands flush against the words.
                string? trimmed = part?.Trim();
                if (string.IsNullOrEmpty(trimmed)) {
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append(separator);
                }
                sb.Append(trimmed);
            }
            return sb.ToString();
        }

        /// <summary>The non-blank lines of a multi-line game string, one buffer line each.</summary>
        public static IEnumerable<string> NonEmptyLines(string? text) {
            if (string.IsNullOrEmpty(text)) {
                yield break;
            }
            foreach (string line in text!.Split('\n')) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    yield return line.Trim();
                }
            }
        }
    }
}
