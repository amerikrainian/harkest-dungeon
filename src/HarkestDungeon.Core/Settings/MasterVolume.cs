using System.Globalization;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The baseline playback volume of every mod sound, in percent: a cue's effective volume is
    /// this value plus that cue's saved offset, so one adjustment moves every sound together
    /// while their relative levels hold. Stepped from the sounds glossary tab, persisted
    /// through the settings store beside the per-sound offsets.
    /// </summary>
    public sealed class MasterVolume : ModSetting {
        public const int DefaultVolume = 100;

        private readonly ISettingsStore _store;

        /// <summary>Current volume in percent, 0..<see cref="SoundVolume.MaxVolume"/>.</summary>
        public int Value { get; private set; }

        public MasterVolume(ISettingsStore store)
            : base("Master", () => Strings.Strings.SettingMasterVolume) {
            _store = store;
            string stored = store.GetString(Key, DefaultVolume.ToString(CultureInfo.InvariantCulture));
            Value = int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? SoundVolume.ClampVolume(parsed)
                : DefaultVolume;
        }

        /// <summary>Step the volume up (+1) or down (-1), clamped to 0..<see
        /// cref="SoundVolume.MaxVolume"/>. Returns whether the value moved; a move persists
        /// immediately.</summary>
        public bool Adjust(int direction) {
            int next = SoundVolume.ClampVolume(Value + direction * SoundVolume.Step);
            if (next == Value) {
                return false;
            }
            Value = next;
            _store.SetString(Key, next.ToString(CultureInfo.InvariantCulture));
            return true;
        }
    }
}
