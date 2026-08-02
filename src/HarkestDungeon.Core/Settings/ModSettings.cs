using System.Collections.Generic;
using DD2A11y.Core.Text;

namespace DD2A11y.Core.Settings {
    /// <summary>
    /// Every mod setting, grouped by the mod tab that lists it, each group in declaration
    /// order: <see cref="General"/> on the mod settings tab, <see cref="Announcements"/>
    /// (the toggles choosing which optional mod announcements speak) on the mod
    /// announcements tab.
    /// </summary>
    public sealed class ModSettings {
        /// <summary>The separator joining the parts of a spoken line ("Exit game, button").</summary>
        public TextSetting Separator { get; }

        /// <summary>How far (road units) the road layer senses its objects - pickup pings and
        /// node identity ticks alike, which keep their relative reach. Read live per scan.</summary>
        public IntSetting SensingRange { get; }

        /// <summary>Whether a corpse's own destruction speaks a died line in battle (a corpse
        /// smashed by a skill or crumbling on its round timer).</summary>
        public BoolSetting CorpseDeaths { get; }

        public IReadOnlyList<ModSetting> General { get; }

        public IReadOnlyList<ModSetting> Announcements { get; }

        public ModSettings(ISettingsStore store) {
            Separator = new TextSetting("spoken_separator", () => Strings.Strings.SettingSeparator,
                ", ", store, value => SpokenLine.Separator = value);
            SensingRange = new IntSetting("sensing_range", () => Strings.Strings.SettingSensingRange,
                80, 20, 200, store);
            CorpseDeaths = new BoolSetting("corpse_deaths", () => Strings.Strings.SettingCorpseDeaths,
                true, store);
            General = new ModSetting[] { Separator, SensingRange };
            Announcements = new ModSetting[] { CorpseDeaths };
        }
    }
}
