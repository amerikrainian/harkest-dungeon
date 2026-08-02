using System.Collections.Generic;
using DD2A11y.Core.Audio;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One sounds-glossary row: the label names what the mod uses the sound for, the value is its
    /// saved volume. Enter toggles the preview loop, Left/Right step the volume through the
    /// advertised increase/decrease actions - while the preview runs, each step is heard live.
    /// </summary>
    public sealed class SoundGlossaryElement : UIElement {
        private readonly SoundVolume _sound;
        private readonly SoundPreview _preview;

        public SoundGlossaryElement(SoundVolume sound, SoundPreview preview) {
            _sound = sound;
            _preview = preview;
        }

        public bool IsPlaying => _preview.Playing == _sound.Cue;

        public override string Status => IsPlaying ? S.StatusPlaying : null;
        public override string Label => _sound.Label;
        public override string Value => S.ValuePercent(_sound.Value);
        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => _preview.Toggle(_sound.Cue));
            yield return new ElementAction(ActionIds.Increase, () => Adjust(+1));
            yield return new ElementAction(ActionIds.Decrease, () => Adjust(-1));
        }

        private void Adjust(int direction) {
            if (_sound.Adjust(direction)) {
                _preview.VolumeChanged();
            }
        }

        // A volume step reads the bare percent; "playing" repeating on every step is noise, and
        // the running preview already carries the change audibly.
        public override string GetAdjustText(string actionId, bool changed)
            => changed ? Value : base.GetAdjustText(actionId, changed);
    }
}
