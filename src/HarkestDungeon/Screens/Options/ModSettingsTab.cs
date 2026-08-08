using DD2A11y.Core.Nav;
using DD2A11y.Core.Settings;
using DD2A11y.Elements;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod settings tab: one row per <see cref="ModSettings.General"/> entry, in
    /// declaration order. Free-text settings edit through the mod's own typing mode;
    /// committing an empty value resets the setting to its default.
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
            foreach (var setting in _settings.General) {
                if (setting is BoolSetting toggle) {
                    items.Add(new ToggleSettingElement(toggle));
                    continue;
                }
                // A numeric setting edits as typed text (any value within its bounds, clamped
                // on commit); nothing commits on garbage, empty restores the default.
                if (setting is IntSetting number) {
                    items.Add(new TextEntryElement(
                        () => number.Label,
                        () => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        typed => {
                            if (typed.Length == 0) {
                                _speak(S.SettingReset, true);
                                number.Reset();
                            } else if (int.TryParse(typed, System.Globalization.NumberStyles.Integer,
                                           System.Globalization.CultureInfo.InvariantCulture, out int parsed)) {
                                number.Set(parsed);
                            }
                        },
                        _textEdit, _speak,
                        hint: () => number.DefaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    continue;
                }
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
