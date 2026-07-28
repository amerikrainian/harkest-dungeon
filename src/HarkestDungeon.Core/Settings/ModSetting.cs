using System;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The shared surface of one mod setting: a stable persistence <see cref="Key"/> (never
    /// spoken) and an authored, spoken <see cref="Label"/>, resolved at read time so a language
    /// switch reads through to the live strings table. <see cref="ModSettings"/> holds every
    /// setting through this base in declaration order, the order the settings tab lists them.
    /// </summary>
    public abstract class ModSetting {
        /// <summary>Stable persistence key (never spoken), e.g. "spoken_separator".</summary>
        public string Key { get; }

        private readonly Func<string> _label;

        public string Label => _label();

        protected ModSetting(string key, Func<string> label) {
            Key = key;
            _label = label;
        }
    }
}
