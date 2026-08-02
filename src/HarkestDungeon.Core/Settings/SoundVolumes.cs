using System;
using System.Collections.Generic;
using DD2A11y.Core.Audio;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The per-sound volume table: one <see cref="SoundVolume"/> per <see cref="AudioCue"/>, in
    /// declaration order (the order the sounds glossary tab lists them). The audio path reads
    /// <see cref="Gain"/> live on every play and loop update, so an adjustment reaches the very
    /// next playback of that cue.
    /// </summary>
    public sealed class SoundVolumes {
        private readonly SoundVolume[] _byCue;

        public IReadOnlyList<SoundVolume> All { get; }

        public SoundVolumes(ISettingsStore store) {
            var cues = (AudioCue[])Enum.GetValues(typeof(AudioCue));
            _byCue = new SoundVolume[cues.Length];
            foreach (var cue in cues) {
                _byCue[(int)cue] = new SoundVolume(cue, store);
            }
            All = _byCue;
        }

        public float Gain(AudioCue cue) => _byCue[(int)cue].Gain;
    }
}
