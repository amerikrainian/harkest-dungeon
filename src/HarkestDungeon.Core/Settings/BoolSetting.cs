using System;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// One on/off mod setting: an authored label, a default, and the current value, loaded from
    /// and persisted through an <see cref="ISettingsStore"/>. Readers take the live
    /// <see cref="Value"/> each use, so a change applies the moment it commits.
    /// </summary>
    public sealed class BoolSetting : ModSetting {
        private readonly ISettingsStore _store;

        public bool DefaultValue { get; }

        public bool Value { get; private set; }

        public BoolSetting(string key, Func<string> label, bool defaultValue, ISettingsStore store)
            : base(key, label) {
            DefaultValue = defaultValue;
            _store = store;
            string stored = store.GetString(key, defaultValue ? "true" : "false");
            Value = bool.TryParse(stored, out bool parsed) ? parsed : defaultValue;
        }

        public void Set(bool value) {
            Value = value;
            _store.SetString(Key, value ? "true" : "false");
        }

        public void Toggle() => Set(!Value);

        public void Reset() => Set(DefaultValue);
    }
}
