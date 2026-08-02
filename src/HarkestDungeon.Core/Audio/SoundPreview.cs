namespace DD2A11y.Core.Audio {
    /// <summary>
    /// The sounds glossary's one preview voice: toggling a row loops its cue centered at full
    /// base volume, played through the volume-scaled engine so what the player hears is exactly
    /// the saved level, and <see cref="VolumeChanged"/> re-aims the running loop as the volume is
    /// stepped. Starting a preview replaces any other; the tab stops it when focus leaves the
    /// playing row and when the tab itself goes away.
    /// </summary>
    public sealed class SoundPreview {
        private readonly IAudioEngine _engine;
        private IAudioLoop? _loop;

        /// <summary>The cue currently looping, or null.</summary>
        public AudioCue? Playing { get; private set; }

        public SoundPreview(IAudioEngine engine) => _engine = engine;

        /// <summary>Start this cue looping, or stop it if it is the one already playing.</summary>
        public void Toggle(AudioCue cue) {
            bool wasPlaying = Playing == cue;
            Stop();
            if (wasPlaying) {
                return;
            }
            Playing = cue;
            _loop = _engine.StartLoop(cue, 1f, 0f);
        }

        /// <summary>Re-aim the running loop so a volume adjustment is heard immediately.</summary>
        public void VolumeChanged() => _loop?.Update(1f, 0f);

        public void Stop() {
            _loop?.Stop();
            _loop = null;
            Playing = null;
        }
    }
}
