using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The title menu (MAIN_MENU game mode; the menu is not on the screen stack). Two phases: the
    /// startup disclaimer (its text plus one continue control - the game unlocks the menu only via
    /// <c>OnMainMenuPress</c>, which the AnyKey listener would normally call), then the button
    /// list, read from the menu's own ordered selectable list.
    /// </summary>
    public sealed class MainMenuScreen : GameScreen {
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, List<Selectable>> SelectablesField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, List<Selectable>>("m_mainMenuSelectables");
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, bool> DisclaimerShownField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, bool>("m_disclaimerShown");
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, Button> ContinueButtonField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, Button>("m_continueButton");
        private static readonly System.Reflection.FieldInfo DisclaimerDirectorField =
            AccessTools.Field(typeof(MainMenuUiScreenBhv), "m_disclaimerDirector");

        private MainMenuUiScreenBhv _menu;
        private Container _root;
        private bool _builtDisclaimer;
        private int _builtCount;

        public override string Name => S.ScreenMainMenu;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.MAIN_MENU || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            if (KingdomMenuScreen.LiveInstance() != null) {
                return null; // the kingdoms scene owns the title menu
            }
            if (_menu == null) {
                _menu = UnityEngine.Object.FindObjectOfType<MainMenuUiScreenBhv>();
            }
            return _menu;
        }

        public override Container BuildRoot(object target) {
            var menu = (MainMenuUiScreenBhv)target;
            // The game's own Escape at the title menu opens the settings screen.
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (SingletonMonoBehaviour<Assets.Code.UI.Managers.CommonUiBhv>.HasInstance()) {
                    SingletonMonoBehaviour<Assets.Code.UI.Managers.CommonUiBhv>.Instance.ShowOptionsMenu(isPrimaryPauseMenu: true);
                }
            });
            Populate(menu);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var menu = (MainMenuUiScreenBhv)target;
            bool disclaimerNow = !DisclaimerShownField(menu);
            if (disclaimerNow != _builtDisclaimer || (!disclaimerNow && CountActive(menu) != _builtCount)) {
                _root.Clear();
                Populate(menu);
            }
            return PauseScreen.LabelArrived(_root, ref _awaitingLabel);
        }

        private bool _awaitingLabel;

        private void Populate(MainMenuUiScreenBhv menu) {
            _builtDisclaimer = !DisclaimerShownField(menu);
            if (_builtDisclaimer) {
                var directorComponent = DisclaimerDirectorField.GetValue(menu) as Component;
                var disclaimerGo = directorComponent != null ? directorComponent.gameObject : null;
                _root.Add(new StaticTextElement(() => UiText.AllText(disclaimerGo)));
                // Reuse the game's own Continue caption for the unlock control.
                var continueButton = ContinueButtonField(menu);
                _root.Add(new ActionElement(
                    () => UiText.FirstLabel(continueButton != null ? continueButton.gameObject : null),
                    S.RoleButton,
                    menu.OnMainMenuPress));
                _builtCount = 0;
                return;
            }

            int count = 0;
            foreach (var selectable in SelectablesField(menu)) {
                if (selectable == null || !selectable.gameObject.activeInHierarchy) {
                    continue;
                }
                if (selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                    continue; // invisible selection anchor, not a real control
                }
                if (!UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue; // decorative hover target with nothing to ever read
                }
                _root.Add(new SelectableElement(selectable));
                count++;
            }
            _builtCount = count;
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
        }

        private static int CountActive(MainMenuUiScreenBhv menu) {
            int count = 0;
            foreach (var selectable in SelectablesField(menu)) {
                if (selectable != null && selectable.gameObject.activeInHierarchy
                    && selectable.GetComponent<SelectOnEmptyFallbackBhv>() == null
                    && UiText.HasAnyTextSource(selectable.gameObject)) {
                    count++;
                }
            }
            return count;
        }
    }
}
