using DD2A11y.Core.Settings;

namespace DD2A11y.Core.Audio {
    /// <summary>
    /// The engine every caller plays through: scales each cue by that cue's saved
    /// <see cref="SoundVolumes"/> gain on top of the volume the caller computed, so the player's
    /// per-sound setting applies to all playback while the natural dynamics stay the caller's.
    /// The gain is read live on every one-shot and every loop update, so an adjustment reaches a
    /// loop already in flight the next time its owner re-aims it.
    /// </summary>
    public sealed class VolumeScaledEngine : IAudioEngine {
        private readonly IAudioEngine _inner;
        private readonly SoundVolumes _volumes;

        public VolumeScaledEngine(IAudioEngine inner, SoundVolumes volumes) {
            _inner = inner;
            _volumes = volumes;
        }

        public bool Available => _inner.Available;

        public void PlayCue(AudioCue cue, float volume, float pan)
            => _inner.PlayCue(cue, volume * _volumes.Gain(cue), pan);

        public IAudioLoop StartLoop(AudioCue cue, float volume, float pan)
            => new ScaledLoop(_inner.StartLoop(cue, volume * _volumes.Gain(cue), pan), _volumes, cue);

        private sealed class ScaledLoop : IAudioLoop {
            private readonly IAudioLoop _inner;
            private readonly SoundVolumes _volumes;
            private readonly AudioCue _cue;

            public ScaledLoop(IAudioLoop inner, SoundVolumes volumes, AudioCue cue) {
                _inner = inner;
                _volumes = volumes;
                _cue = cue;
            }

            public void Update(float volume, float pan)
                => _inner.Update(volume * _volumes.Gain(_cue), pan);

            public void Stop() => _inner.Stop();
        }
    }
}
