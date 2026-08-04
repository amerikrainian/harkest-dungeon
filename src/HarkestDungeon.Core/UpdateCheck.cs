using System.Text.RegularExpressions;

namespace DD2A11y.Core {
    /// <summary>
    /// The version logic behind the startup update announcement: what version a GitHub
    /// latest-release payload names, and whether it outranks the running build. The fetch
    /// itself lives in the plugin; this stays pure so the comparison rules are unit-tested.
    /// </summary>
    public static class UpdateCheck {
        private static readonly Regex TagPattern =
            new Regex("\"tag_name\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

        /// <summary>The version the release payload names: its tag_name value with any
        /// leading v stripped, or null when the payload carries none.</summary>
        public static string? LatestVersion(string releaseJson) {
            var match = TagPattern.Match(releaseJson);
            if (!match.Success) {
                return null;
            }
            string tag = match.Groups[1].Value.Trim();
            if (tag.Length > 0 && (tag[0] == 'v' || tag[0] == 'V')) {
                tag = tag.Substring(1);
            }
            return tag.Length == 0 ? null : tag;
        }

        /// <summary>Strict greater-than on dot-separated numeric components. Missing
        /// components count as zero ("1.0" equals "1") and non-numeric ones parse as zero,
        /// so a malformed tag compares safe instead of throwing.</summary>
        public static bool IsNewer(string remote, string local) {
            string[] r = remote.Split('.');
            string[] l = local.Split('.');
            int length = r.Length > l.Length ? r.Length : l.Length;
            for (int i = 0; i < length; i++) {
                int remotePart = Component(r, i);
                int localPart = Component(l, i);
                if (remotePart != localPart) {
                    return remotePart > localPart;
                }
            }
            return false;
        }

        private static int Component(string[] parts, int index)
            => index < parts.Length && int.TryParse(parts[index], out int value) ? value : 0;
    }
}
