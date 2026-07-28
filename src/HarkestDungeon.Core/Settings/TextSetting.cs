using System;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// One free-text mod setting: an authored label, a default, and the current value, loaded
    /// from and persisted through an <see cref="ISettingsStore"/>. This is the mod's own state,
    /// not game state, so it is legitimately held. The optional apply hook pushes the value into
    /// the feature it drives, on load and on every change.
    /// </summary>
    public sealed class TextSetting : ModSetting {
        private readonly ISettingsStore _store;
        private readonly Action<string>? _apply;

        public string DefaultValue { get; }

        public string Value { get; private set; }

        public TextSetting(string key, Func<string> label, string defaultValue,
                           ISettingsStore store, Action<string>? apply = null)
            : base(key, label) {
            DefaultValue = defaultValue;
            _store = store;
            _apply = apply;
            Value = store.GetString(key, defaultValue);
            _apply?.Invoke(Value);
        }

        public void Set(string value) {
            Value = value;
            _store.SetString(Key, value);
            _apply?.Invoke(value);
        }

        public void Reset() => Set(DefaultValue);
    }
}
