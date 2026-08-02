using System.Collections.Generic;
using BepInEx.Configuration;
using DD2A11y.Core.Settings;

namespace DD2A11y.Settings {
    /// <summary>
    /// Persists the mod's settings through BepInEx's <see cref="ConfigFile"/> (the plugin's own
    /// Config), so each setting lands in one file under BepInEx/config that survives restarts and
    /// is editable by hand. Each key binds a <see cref="ConfigEntry{T}"/> once and is then reused;
    /// setting its value auto-saves the file.
    /// </summary>
    public sealed class BepInExSettingsStore : ISettingsStore {
        private readonly string _section;
        private readonly ConfigFile _config;
        private readonly Dictionary<string, ConfigEntry<string>> _strings =
            new Dictionary<string, ConfigEntry<string>>();

        public BepInExSettingsStore(ConfigFile config, string section = "Settings") {
            _config = config;
            _section = section;
        }

        private ConfigEntry<string> Bind(string key, string defaultValue) {
            if (!_strings.TryGetValue(key, out var entry)) {
                entry = _config.Bind(_section, key, Quote(defaultValue));
                _strings[key] = entry;
            }
            return entry;
        }

        public string GetString(string key, string defaultValue) => Unquote(Bind(key, defaultValue).Value);

        public void SetString(string key, string value) => Bind(key, value).Value = Quote(value);

        // The config parser trims values on reload, which would eat a separator's edge spaces
        // (" - "); quote-wrapping carries them through the round trip. Only values that need it
        // are wrapped (edge whitespace, or already quote-delimited so the unwrap round-trips),
        // keeping plain values - the sound volumes - hand-editable numbers in the file.
        private static string Quote(string value) => NeedsQuotes(value) ? "\"" + value + "\"" : value;

        private static bool NeedsQuotes(string value)
            => value.Length > 0
               && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])
                   || (value[0] == '"' && value[value.Length - 1] == '"'));

        private static string Unquote(string value) {
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}
