using System.Globalization;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The saved playback volume of one mod sound, in percent of the cue's natural level: 100
    /// plays the sound exactly as authored, lower scales every playback of that one cue while its
    /// natural dynamics (distance attenuation, pan) still apply on top. Stepped from the sounds
    /// glossary tab, persisted through the settings store under the cue's name.
    /// </summary>
    public sealed class SoundVolume : ModSetting {
        public const int DefaultVolume = 100;
        public const int Step = 10;

        private readonly ISettingsStore _store;

        public Audio.AudioCue Cue { get; }

        /// <summary>Current volume in percent, 0..100.</summary>
        public int Value { get; private set; }

        /// <summary>The gain factor applied to the cue's natural playback volume.</summary>
        public float Gain => Value / 100f;

        public SoundVolume(Audio.AudioCue cue, ISettingsStore store)
            : base(cue.ToString(), () => Strings.Strings.SoundLabel(cue)) {
            Cue = cue;
            _store = store;
            string stored = store.GetString(Key, DefaultVolume.ToString(CultureInfo.InvariantCulture));
            Value = int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? Clamp(parsed)
                : DefaultVolume;
        }

        /// <summary>Step the volume up (+1) or down (-1), clamped to 0..100. Returns whether the
        /// value moved; a move persists immediately.</summary>
        public bool Adjust(int direction) {
            int next = Clamp(Value + direction * Step);
            if (next == Value) {
                return false;
            }
            Value = next;
            _store.SetString(Key, next.ToString(CultureInfo.InvariantCulture));
            return true;
        }

        private static int Clamp(int value) => value < 0 ? 0 : value > 100 ? 100 : value;
    }
}
