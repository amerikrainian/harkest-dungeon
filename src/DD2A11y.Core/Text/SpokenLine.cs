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
                if (string.IsNullOrEmpty(part)) {
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append(separator);
                }
                sb.Append(part);
            }
            return sb.ToString();
        }
    }
}
