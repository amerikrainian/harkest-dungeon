using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One on/off mod setting as a row, reading like the game's own settings toggles: label,
    /// "toggle", then the on/off state. Enter flips and persists the value; the re-announce
    /// speaks the new state.
    /// </summary>
    public sealed class ToggleSettingElement : UIElement {
        private readonly BoolSetting _setting;

        public ToggleSettingElement(BoolSetting setting) {
            _setting = setting;
        }

        public override string Label => _setting.Label;

        public override string Role => S.RoleToggle;

        public override string Status => _setting.Value ? S.StatusOn : S.StatusOff;

        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _setting.Toggle);
        }
    }
}
