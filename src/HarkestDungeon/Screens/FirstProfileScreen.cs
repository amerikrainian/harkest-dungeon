using System;
using Assets.Code.Game;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The first-boot profile window (the game's GDPR panel): with no profile on disk, the game
    /// auto-creates a default one and holds the title menu behind this widget - name, language,
    /// analytics consent, continue - with every menu button disabled, which used to read as a
    /// menu that ignored Enter. Same widget as the profile-select create window, built by the
    /// shared tree. The game offers no cancel here, so Escape reports unavailable; the name edit
    /// is already active on entry, keystrokes echo, and the accepted name reads back.
    /// </summary>
    public sealed class FirstProfileScreen : GameScreen {
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, ProfileCreationWidgetBhv> CreationField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, ProfileCreationWidgetBhv>("m_firstTimeProfileCreationBhv");

        private readonly Action<string, bool> _speak;
        private readonly Core.Text.TypingEcho _echo;
        private MainMenuUiScreenBhv _menu;
        private ProfileCreationWidgetBhv _creation;

        public FirstProfileScreen(Action<string, bool> speak) {
            _speak = speak;
            _echo = new Core.Text.TypingEcho(() => EditingName, TypedText, speak);
        }

        public override string Name
            => GameLoc.TryGet("first_time_profile_creation_title") ?? S.ScreenProfileSelect;

        /// <summary>Whether the window's name field is capturing keystrokes; the input manager
        /// pauses the mod's keys through this, like the profile-select edits.</summary>
        public bool EditingName {
            get {
                var field = ProfileCreationTree.NameField(_creation);
                return field != null && field.isFocused;
            }
        }

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.MAIN_MENU
                || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            if (_menu == null) {
                _menu = UnityEngine.Object.FindObjectOfType<MainMenuUiScreenBhv>();
            }
            var creation = _menu != null ? CreationField(_menu) : null;
            return creation != null && creation.gameObject.activeInHierarchy ? creation : null;
        }

        public override Container BuildRoot(object target) {
            _creation = (ProfileCreationWidgetBhv)target;
            // The game offers no way out of the first-boot window; a profile must be created.
            var root = new RootContainer(ContainerShape.VerticalList,
                back: () => _speak(S.StatusUnavailable, true));
            ProfileCreationTree.Populate(root, _creation, includeTitle: false);
            return root;
        }

        public override bool OnUpdate(object target) {
            if (_echo.Tick()) {
                _speak(TypedText(), true);
            }
            return false;
        }

        private string TypedText() {
            var field = ProfileCreationTree.NameField(_creation);
            return field != null ? field.text : "";
        }
    }
}
