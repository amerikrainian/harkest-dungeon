using System.Collections.Generic;
using DD2A11y.Core.Text;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// Every mod setting, in the order the settings tab lists them.
    /// </summary>
    public sealed class ModSettings {
        /// <summary>The separator joining the parts of a spoken line ("Exit game, button").</summary>
        public TextSetting Separator { get; }

        /// <summary>How far (road units) the road layer senses its objects - pickup pings and
        /// node identity ticks alike, which keep their relative reach. Read live per scan.</summary>
        public IntSetting SensingRange { get; }

        public IReadOnlyList<ModSetting> All { get; }

        public ModSettings(ISettingsStore store) {
            Separator = new TextSetting("spoken_separator", () => Strings.Strings.SettingSeparator,
                ", ", store, value => SpokenLine.Separator = value);
            SensingRange = new IntSetting("sensing_range", () => Strings.Strings.SettingSensingRange,
                80, 20, 200, store);
            All = new ModSetting[] { Separator, SensingRange };
        }
    }
}
