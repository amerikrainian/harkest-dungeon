using System;
using System.Collections.Generic;
using DD2A11y.Core.Audio;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// The per-sound volume table: the <see cref="Master"/> baseline plus one
    /// <see cref="SoundVolume"/> per <see cref="AudioCue"/>, in declaration order (the order the
    /// sounds glossary tab lists them). The audio path reads <see cref="Gain"/> live on every
    /// play and loop update, so an adjustment - master or per-sound - reaches the very next
    /// playback of every cue.
    /// </summary>
    public sealed class SoundVolumes {
        private readonly SoundVolume[] _byCue;

        /// <summary>The baseline volume every cue's offset rides on.</summary>
        public MasterVolume Master { get; }

        public IReadOnlyList<SoundVolume> All { get; }

        public SoundVolumes(ISettingsStore store) {
            Master = new MasterVolume(store);
            var cues = (AudioCue[])Enum.GetValues(typeof(AudioCue));
            _byCue = new SoundVolume[cues.Length];
            foreach (var cue in cues) {
                _byCue[(int)cue] = new SoundVolume(cue, store, Master);
            }
            All = _byCue;
        }

        public float Gain(AudioCue cue) => _byCue[(int)cue].Gain;
    }
}
