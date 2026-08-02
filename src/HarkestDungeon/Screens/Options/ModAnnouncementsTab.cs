using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod announcements tab: one toggle per optional mod announcement, in
    /// <see cref="ModSettings.Announcements"/> declaration order.
    /// </summary>
    public sealed class ModAnnouncementsTab : ModTab {
        private readonly ModSettings _settings;

        public ModAnnouncementsTab(ModSettings settings) {
            _settings = settings;
        }

        public override string Name => S.TabModAnnouncements;

        public override void Populate(Container items) {
            foreach (var setting in _settings.Announcements) {
                if (setting is BoolSetting toggle) {
                    items.Add(new ToggleSettingElement(toggle));
                }
            }
        }
    }
}
