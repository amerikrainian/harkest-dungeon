namespace DD2A11y.Core.Audio {
    /// <summary>
    /// The sounds glossary's preview voice, centered at full base volume through the
    /// volume-scaled engine so what the player hears is exactly the saved level: a one-shot
    /// per press, or a loop toggled on and off, with <see cref="VolumeChanged"/> re-aiming a
    /// running loop as the volume is stepped. Starting either replaces a running loop; the tab
    /// stops the loop when focus leaves the playing row and when the tab itself goes away. The
    /// sound alone is the feedback - previews never speak.
    /// </summary>
    public sealed class SoundPreview {
        private readonly IAudioEngine _engine;
        private IAudioLoop? _loop;

        /// <summary>The cue currently looping, or null.</summary>
        public AudioCue? Playing { get; private set; }

        public SoundPreview(IAudioEngine engine) => _engine = engine;

        /// <summary>Play this cue once (a running loop stops first, so one instance plays).</summary>
        public void PlayOnce(AudioCue cue) {
            Stop();
            _engine.PlayCue(cue, 1f, 0f);
        }

        /// <summary>Start this cue looping, or stop it if it is the one already looping.</summary>
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
