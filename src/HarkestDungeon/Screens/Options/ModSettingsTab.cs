using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using DD2A11y.Elements;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod settings tab: one row per <see cref="ModSettings"/> entry, in declaration order.
    /// Free-text settings edit through the mod's own typing mode; committing an empty value
    /// resets the setting to its default.
    /// </summary>
    public sealed class ModSettingsTab : ModTab {
        private readonly ModSettings _settings;
        private readonly ModTextEdit _textEdit;
        private readonly System.Action<string, bool> _speak;

        public ModSettingsTab(ModSettings settings, ModTextEdit textEdit,
                              System.Action<string, bool> speak) {
            _settings = settings;
            _textEdit = textEdit;
            _speak = speak;
        }

        public override string Name => S.TabModSettings;

        public override void Populate(Container items) {
            foreach (var setting in _settings.All) {
                if (setting is TextSetting text) {
                    items.Add(new TextEntryElement(
                        () => text.Label,
                        () => Core.Text.SpokenChars.Spell(text.Value),
                        typed => {
                            if (typed.Length == 0) {
                                _speak(S.SettingReset, true);
                                text.Reset();
                            } else {
                                text.Set(typed);
                            }
                        },
                        _textEdit, _speak,
                        hint: () => Core.Text.SpokenChars.Spell(text.DefaultValue)));
                }
            }
        }
    }
}
