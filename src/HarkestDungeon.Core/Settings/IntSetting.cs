using System;
using System.Globalization;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// One numeric mod setting: an authored label, a default, and clamping bounds, loaded from
    /// and persisted through an <see cref="ISettingsStore"/>. Edited as typed text (any value
    /// within the bounds, not stepped). Readers take the live <see cref="Value"/> each use, so
    /// a change applies the moment it commits.
    /// </summary>
    public sealed class IntSetting : ModSetting {
        private readonly ISettingsStore _store;

        public int DefaultValue { get; }
        public int Minimum { get; }
        public int Maximum { get; }

        public int Value { get; private set; }

        public IntSetting(string key, Func<string> label, int defaultValue,
                          int minimum, int maximum, ISettingsStore store)
            : base(key, label) {
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
            _store = store;
            string stored = store.GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));
            Value = int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? Clamp(parsed)
                : defaultValue;
        }

        /// <summary>Set (clamped to the bounds) and persist.</summary>
        public void Set(int value) {
            Value = Clamp(value);
            _store.SetString(Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        public void Reset() => Set(DefaultValue);

        private int Clamp(int value) => value < Minimum ? Minimum : value > Maximum ? Maximum : value;
    }
}
