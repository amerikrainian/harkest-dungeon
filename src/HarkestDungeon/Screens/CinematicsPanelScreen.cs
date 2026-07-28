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
    /// The title menu's Watch Cinematics panel - not a stack screen: a timeline-animated panel
    /// on the menu itself that locks every menu button while it is up (which is why the menu's
    /// own tree reads as bare unavailable buttons underneath it). One vertical list: the
    /// unlocked cinematic buttons, then the panel's own Back. Escape closes through the game's
    /// <see cref="MainMenuUiScreenBhv.CloseCinematicPanel"/>. Playing a cinematic switches the
    /// game to CINEMATIC mode, so this screen stands down for the video - releasing the
    /// keyboard to the game's own skip handling - and takes over again on the revert.
    /// </summary>
    public sealed class CinematicsPanelScreen : GameScreen {
        private static readonly System.Reflection.FieldInfo DirectorField =
            AccessTools.Field(typeof(MainMenuUiScreenBhv), "m_cinematicDirector");

        private MainMenuUiScreenBhv _menu;
        private Container _root;

        public override string Name => GameLoc.TryGet("main_menu_watch_cinematics_title") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.MAIN_MENU
                || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            if (_menu == null) {
                _menu = Object.FindObjectOfType<MainMenuUiScreenBhv>();
            }
            if (_menu == null || !_menu.IsCinematicPanelActive()) {
                return null;
            }
            var director = DirectorField.GetValue(_menu) as Component;
            var panel = director != null ? director.gameObject : null;
            // The open timeline fades the buttons in; hold the takeover until the landing
            // button reads cleanly rather than enter on a bare "button, unavailable".
            if (panel == null || !LandingReady(panel)) {
                return null;
            }
            return panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (GameObject)target;
            var menu = _menu;
            _root = new RootContainer(ContainerShape.VerticalList, back: menu.CloseCinematicPanel);
            foreach (var selectable in Sweep(panel)) {
                _root.Add(new SelectableElement(selectable));
            }
            return _root;
        }

        private static bool LandingReady(GameObject panel) {
            foreach (var selectable in Sweep(panel)) {
                return selectable.IsInteractable()
                    && !string.IsNullOrEmpty(UiText.FirstLabel(selectable.gameObject));
            }
            return false;
        }

        private static IEnumerable<Selectable> Sweep(GameObject panel) {
            foreach (var selectable in panel.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                    continue;
                }
                if (!UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue;
                }
                yield return selectable;
            }
        }
    }
}
