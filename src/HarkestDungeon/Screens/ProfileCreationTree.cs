using System.Collections.Generic;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The profile-creation widget as a navigable tree - name field, language dropdown, the GDPR
    /// text around the analytics toggle, then the buttons - shared by the profile-select create
    /// window and the first-boot profile window, which show the same widget.
    /// </summary>
    internal static class ProfileCreationTree {
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, TMP_InputField> NameFieldRef =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, TMP_InputField>("m_nameInputLabel");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, LanguageWidgetBhv> LanguageField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, LanguageWidgetBhv>("m_languageDropdownBhv");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, Toggle> AnalyticsToggleField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, Toggle>("m_analyticsToggle");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, Button> CreateButtonField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, Button>("m_createButton");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, TextMeshProUGUI> AnalyticsLabelField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, TextMeshProUGUI>("m_analyticsCheckLabel");

        /// <summary>The widget's name input, for typing detection and read-back.</summary>
        public static TMP_InputField NameField(ProfileCreationWidgetBhv creation)
            => creation != null ? NameFieldRef(creation) : null;

        /// <summary>Build the window top to bottom as the game lays it out. The title is skipped
        /// where the screen's own name already carries it (the first-boot window).</summary>
        public static void Populate(Container root, ProfileCreationWidgetBhv creation, bool includeTitle) {
            if (includeTitle) {
                root.Add(new StaticTextElement(() => GameLoc.TryGet("first_time_profile_creation_title")));
            }
            root.Add(new ActionElement(
                () => GameLoc.TryGet("profile_creation_name_label"),
                S.RoleEdit,
                creation.OnEditNameButtonPressed,
                extraBufferLines: () => BufferLine(GameLoc.TryGet("profile_creation_name_char_limit_label")),
                value: () => {
                    var field = NameFieldRef(creation);
                    return field != null ? field.text : null;
                }));
            var language = LanguageField(creation);
            var dropdown = language != null
                ? language.GetComponentInChildren<TMP_Dropdown>(includeInactive: true) : null;
            if (dropdown != null) {
                root.Add(new SelectableElement(dropdown,
                    () => GameLoc.TryGet("profile_creation_language_label")));
            }
            root.Add(new StaticTextElement(() => GameLoc.TryGet("gdpr_dialog_desc_1")));
            var toggle = AnalyticsToggleField(creation);
            root.Add(new SelectableElement(toggle, () => {
                var label = AnalyticsLabelField(creation);
                return label != null ? label.text : null;
            }));
            root.Add(new StaticTextElement(() => GameLoc.TryGet("gdpr_dialog_desc_2")));
            var create = CreateButtonField(creation);
            root.Add(new SelectableElement(create));
            foreach (var button in create.transform.parent.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button != create) {
                    root.Add(new SelectableElement(button));
                }
            }
        }

        private static IEnumerable<string> BufferLine(string line) {
            if (!string.IsNullOrEmpty(line)) {
                yield return line;
            }
        }
    }
}
