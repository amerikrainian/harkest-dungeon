using Assets.Code.Locale;
using Assets.Code.Utils;

namespace DD2A11y.Game {
    /// <summary>Access to the game's own localized strings. Always prefer these over authored
    /// text - they stay current across game updates and translate with the game language.</summary>
    public static class GameLoc {
        /// <summary>The localized string for a key, or null when the key is empty or missing (the
        /// game's GetString would return a cyan-colored placeholder instead).</summary>
        public static string TryGet(string locKey) {
            if (string.IsNullOrEmpty(locKey)) {
                return null;
            }
            var loc = Singleton<Localization>.Instance;
            if (loc == null) {
                return null;
            }
            string value = loc.TryGetString(locKey);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private const string MissingKeyPrefix = "<color=#00FFFF>";
        private const string MissingKeySuffix = "</color>";

        /// <summary>A game-composed string (a DataContext binding filled via GetString) with the
        /// game's colored missing-key marker nulled out - GetString wraps a missing key as
        /// "&lt;color=#00FFFF&gt;key&lt;/color&gt;" (its GetColoredMissingKey), and a raw loc key
        /// is never worth speaking.</summary>
        public static string DropMissingKeyMarker(string value) {
            if (value != null
                && value.StartsWith(MissingKeyPrefix, System.StringComparison.Ordinal)
                && value.EndsWith(MissingKeySuffix, System.StringComparison.Ordinal)
                && value.IndexOf('<', MissingKeyPrefix.Length)
                    == value.Length - MissingKeySuffix.Length) {
                return null;
            }
            return value;
        }
    }
}
