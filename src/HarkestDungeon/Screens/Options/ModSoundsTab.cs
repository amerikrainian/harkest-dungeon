using DD2A11y.Core.Audio;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod sounds glossary tab: one row per <see cref="AudioCue"/> naming what the sound is
    /// used for. Enter loops the row's sound as a preview, Left/Right step its saved per-sound
    /// volume (a running preview re-aims live). The preview stops when focus leaves the playing
    /// row, on a tab switch, and when the screen closes.
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
            foreach (var sound in _sounds.All) {
                items.Add(new SoundGlossaryElement(sound, _preview));
            }
        }

        public override void OnHidden() => _preview.Stop();
    }
}
