using DD2A11y.Core.Audio;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod sounds glossary tab: the master volume slider, then one row per
    /// <see cref="AudioCue"/> naming what the sound is used for. Enter plays a row's sound once,
    /// Space loops it (both silent - the sound is the feedback), Left/Right step its saved
    /// per-sound volume (a running loop re-aims live); on the master row they step the baseline
    /// every sound rides on. The loop stops when focus leaves the playing row, on a tab switch,
    /// and when the screen closes.
    /// </summary>
    public sealed class ModSoundsTab : ModTab {
        private readonly SoundVolumes _sounds;
        private readonly SoundPreview _preview;

        public ModSoundsTab(SoundVolumes sounds, IAudioEngine audio, Navigator navigator) {
            _sounds = sounds;
            _preview = new SoundPreview(audio);
            navigator.FocusSettled += element => {
                if (_preview.Playing != null
                    && !(element is SoundGlossaryElement row && row.IsPlaying)) {
                    _preview.Stop();
                }
            };
        }

        public override string Name => S.TabModSounds;

        public override void Populate(Container items) {
            items.Add(new MasterVolumeElement(_sounds.Master));
            foreach (var sound in _sounds.All) {
                items.Add(new SoundGlossaryElement(sound, _preview));
            }
        }

        public override void OnHidden() => _preview.Stop();
    }
}
