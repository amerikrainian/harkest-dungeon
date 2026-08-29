using DD2A11y.Core.Audio;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod sounds glossary tab: the master volume slider, then a group tab per sound
    /// family (road, combat - the assets/audio folders), then one row per
    /// <see cref="AudioCue"/> in the active group naming what the sound is used for.
    /// Left/Right on the group tab switch groups; Enter plays a row's sound once, Space loops
    /// it (both silent - the sound is the feedback), Left/Right step its saved per-sound
    /// volume (a running loop re-aims live); on the master row they step the baseline every
    /// sound rides on. The loop stops when focus leaves the playing row, on a group or tab
    /// switch, and when the screen closes.
    /// </summary>
    public sealed class ModSoundsTab : ModTab {
        private static readonly AudioCueGroup[] Groups =
            { AudioCueGroup.Road, AudioCueGroup.Combat, AudioCueGroup.Crossroads };

        private readonly SoundVolumes _sounds;
        private readonly SoundPreview _preview;
        private int _group;
        private Container _rows;

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
            _group = 0;
            items.Add(new MasterVolumeElement(_sounds.Master));
            items.Add(new TabSelectorElement(() => _group, () => Groups.Length, GroupName, Select));
            _rows = new Container(ContainerShape.VerticalList);
            items.Add(_rows);
            PopulateRows();
        }

        private static string GroupName(int index) {
            switch (Groups[index]) {
                case AudioCueGroup.Combat: return S.SoundTabCombat;
                case AudioCueGroup.Crossroads: return S.SoundTabCrossroads;
                default: return S.SoundTabRoad;
            }
        }

        private void Select(int index) {
            _group = index;
            _preview.Stop();
            PopulateRows();
        }

        private void PopulateRows() {
            _rows.Clear();
            foreach (var sound in _sounds.All) {
                if (AudioCues.GroupOf(sound.Cue) == Groups[_group]) {
                    _rows.Add(new SoundGlossaryElement(sound, _preview));
                }
            }
        }

        public override void OnHidden() => _preview.Stop();
    }
}
