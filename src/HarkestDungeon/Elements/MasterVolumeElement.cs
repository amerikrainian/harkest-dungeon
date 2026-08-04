using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// The master volume row heading the mod sounds glossary tab: Left/Right step the baseline
    /// volume of every mod sound, with the per-sound volumes below riding on it as offsets. A
    /// step reads the bare percent.
    /// </summary>
    public sealed class MasterVolumeElement : UIElement {
        private readonly MasterVolume _master;

        public MasterVolumeElement(MasterVolume master) {
            _master = master;
        }

        public override string Label => _master.Label;

        public override string Role => S.RoleSlider;

        public override string Value => S.ValuePercent(_master.Value);

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Increase, () => _master.Adjust(+1));
            yield return new ElementAction(ActionIds.Decrease, () => _master.Adjust(-1));
        }

        public override string GetAdjustText(string actionId, bool changed)
            => changed ? Value : base.GetAdjustText(actionId, changed);
    }
}
