namespace DD2A11y.Core.Audio {
    /// <summary>
    /// The cue playback backend: the mod's OWN audio output, independent of the game's FMOD
    /// mixer, so cues are never ducked or colored by it. Kept an interface so Core stays
    /// engine-free; the implementation (NAudio, ported from the NonVisualCalculus/WOTR engine)
    /// lives in the plugin. Callers compute placement themselves and hand the finished pan.
    /// </summary>
    public interface IAudioEngine {
        /// <summary>Whether the output device is usable (false once it failed to open or died).</summary>
        bool Available { get; }

        /// <summary>Fire a one-shot cue at <paramref name="volume"/> (0..1) and stereo
        /// <paramref name="pan"/> (-1 hard left .. 1 hard right). Missing device or asset means
        /// silence (logged by the engine), never a throw.</summary>
        void PlayCue(AudioCue cue, float volume, float pan);
    }
}
