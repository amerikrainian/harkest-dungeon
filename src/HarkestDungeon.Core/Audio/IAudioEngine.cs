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

        /// <summary>Start a cue looping and return its live handle: the caller re-aims it with
        /// <see cref="IAudioLoop.Update"/> as the world moves (every frame is fine - the voice
        /// smooths parameter steps itself) and ends it with <see cref="IAudioLoop.Stop"/>.
        /// Missing device or asset returns an inert handle, never null and never a throw.</summary>
        IAudioLoop StartLoop(AudioCue cue, float volume, float pan);
    }

    /// <summary>A live looping voice. Never null; an engine without a device hands out an inert
    /// one so callers keep a single code path.</summary>
    public interface IAudioLoop {
        /// <summary>Re-aim the loop (same ranges as <see cref="IAudioEngine.PlayCue"/>).</summary>
        void Update(float volume, float pan);

        /// <summary>End the loop (a short internal fade avoids a cut-off click). The handle is
        /// dead afterwards; start a new loop to resume.</summary>
        void Stop();
    }
}
