using System.Collections.Generic;
using DD2A11y.Core.Text;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// Every mod setting, in the order the settings tab lists them.
    /// </summary>
    public sealed class ModSettings {
        /// <summary>The separator joining the parts of a spoken line ("Exit game, button").</summary>
        public TextSetting Separator { get; }

        public IReadOnlyList<ModSetting> All { get; }

        public ModSettings(ISettingsStore store) {
            Separator = new TextSetting("spoken_separator", () => Strings.Strings.SettingSeparator,
                ", ", store, value => SpokenLine.Separator = value);
            All = new ModSetting[] { Separator };
        }
    }
}
