using System.Globalization;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The saved playback volume of one mod sound, stored as a signed offset from the master
    /// volume: the effective (and displayed) <see cref="Value"/> is master plus the offset,
    /// clamped to 0..<see cref="MaxVolume"/> percent of the cue's natural level, with the cue's
    /// natural dynamics (distance attenuation, pan) still applying on top. Stepping from the
    /// sounds glossary tab re-derives the offset against the current master, so a later master
    /// move carries every sound with it while their relative levels hold. Persisted through the
    /// settings store under the cue's name.
    /// </summary>
    public sealed class SoundVolume : ModSetting {
        public const int Step = 10;
        public const int MaxVolume = 200;

        private readonly ISettingsStore _store;
        private readonly MasterVolume _master;
        private int _offset;

        public Audio.AudioCue Cue { get; }

        /// <summary>Effective volume in percent, 0..<see cref="MaxVolume"/>: the master volume
        /// plus this sound's offset.</summary>
        public int Value => ClampVolume(_master.Value + _offset);

        /// <summary>The gain factor applied to the cue's natural playback volume.</summary>
        public float Gain => Value / 100f;

        public SoundVolume(Audio.AudioCue cue, ISettingsStore store, MasterVolume master)
            : base(cue.ToString(), () => Strings.Strings.SoundLabel(cue)) {
            Cue = cue;
            _store = store;
            _master = master;
            _offset = ParseOffset(store.GetString(Key, FormatOffset(0)));
        }

        /// <summary>Step the effective volume up (+1) or down (-1), clamped to
        /// 0..<see cref="MaxVolume"/>. Returns whether the value moved; a move persists
        /// immediately.</summary>
        public bool Adjust(int direction) {
            int next = ClampVolume(Value + direction * Step);
            if (next == Value) {
                return false;
            }
            _offset = next - _master.Value;
            _store.SetString(Key, FormatOffset(_offset));
            return true;
        }

        internal static int ClampVolume(int value)
            => value < 0 ? 0 : value > MaxVolume ? MaxVolume : value;

        // Offsets persist with an explicit sign ("+10", "-40", "+0"); a bare number is a value
        // from before the master volume existed - an absolute percent, equivalent to an offset
        // from a master at its default.
        private static string FormatOffset(int offset)
            => offset.ToString("+0;-0", CultureInfo.InvariantCulture);

        private static int ParseOffset(string stored) {
            stored = stored.Trim();
            if (!int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) {
                return 0;
            }
            if (stored[0] == '+' || stored[0] == '-') {
                return parsed;
            }
            return ClampVolume(parsed) - MasterVolume.DefaultVolume;
        }
    }
}
