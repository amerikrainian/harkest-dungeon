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
    }
}
