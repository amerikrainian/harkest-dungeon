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
    /// <c>OnMainMenuPress</c>, which the AnyKey listener would normally call), then the buttons in
    /// their VISUAL order - the main stack top to bottom, then the footer row left to right. The
    /// menu's serialized selectable list groups the footer first, which read the menu upside down.
    /// </summary>
    public sealed class MainMenuScreen : GameScreen {
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, List<Selectable>> SelectablesField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, List<Selectable>>("m_mainMenuSelectables");
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, bool> DisclaimerShownField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, bool>("m_disclaimerShown");
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, Button> ContinueButtonField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, Button>("m_continueButton");
        private static readonly AccessTools.FieldRef<MainMenuUiScreenBhv, Button> ProfileButtonField =
            AccessTools.FieldRefAccess<MainMenuUiScreenBhv, Button>("m_profileSelectButton");
        private static readonly System.Reflection.FieldInfo DisclaimerDirectorField =
            AccessTools.Field(typeof(MainMenuUiScreenBhv), "m_disclaimerDirector");

        private MainMenuUiScreenBhv _menu;
        private Container _root;
        private bool _builtDisclaimer;
        private int _builtOrder;
        private int _lastSeenOrder;
        private readonly Dictionary<Selectable, UIElement> _elements = new Dictionary<Selectable, UIElement>();

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
            _elements.Clear();
            Populate(menu);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var menu = (MainMenuUiScreenBhv)target;
            bool disclaimerNow = !DisclaimerShownField(menu);
            // The order signature covers the active set AND its visual order: the open
            // animation slides the buttons in, so the entry build can sort transient
            // positions - the settle re-sorts silently (elements are reused per button, so
            // focus holds and nothing re-announces). A changed signature rebuilds only once
            // it holds for two frames: the Confessions submenu swap deactivates the main
            // stack a frame before its own buttons arrive, and a rebuild caught mid-swap
            // re-homed onto the side promo instead of the submenu's first entry.
            int order = disclaimerNow ? 0 : OrderSignature(menu);
            bool orderSettled = order == _lastSeenOrder;
            _lastSeenOrder = order;
            if (disclaimerNow != _builtDisclaimer || (!disclaimerNow && orderSettled && order != _builtOrder)) {
                // Through the menu's open animation the buttons are disabled with their tooltip
                // captions off, and the unlock staggers across frames, so a rebuild would land
                // on a bare "button, unavailable". Hold the tree until the landing button is
                // readable; a keypress meanwhile skips the animation, the game's own behavior.
                if (!disclaimerNow && !LandingReady(menu)) {
                    return false;
                }
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
                _builtOrder = 0;
                return;
            }

            if (!LandingReady(menu)) {
                // Entered mid-animation (returning from a run pans back into the menu with the
                // buttons locked); leave the tree empty - the signature mismatch re-runs this
                // once the buttons unlock.
                _builtOrder = -1;
                _awaitingLabel = false;
                return;
            }

            var profileButton = ProfileButtonField(menu);
            foreach (var selectable in Swept(menu)) {
                if (!_elements.TryGetValue(selectable, out var element)) {
                    // The profile button (the bottom-right journal) labels itself with the
                    // CURRENT PROFILE'S NAME; its purpose lives only in its tooltip, so that
                    // rides as the value ("Darkest, button, Change Profile").
                    element = selectable == profileButton
                        ? new SelectableElement(selectable, value: () => FirstTooltip(selectable))
                        : new SelectableElement(selectable);
                    _elements[selectable] = element;
                }
                _root.Add(element);
            }
            _builtOrder = OrderSignature(menu);
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
        }

        private static string FirstTooltip(Selectable selectable) {
            foreach (var line in TooltipReader.Lines(selectable.gameObject)) {
                return line;
            }
            return null;
        }

        private static int OrderSignature(MainMenuUiScreenBhv menu) {
            int signature = 17;
            foreach (var selectable in Swept(menu)) {
                signature = signature * 31 + selectable.GetInstanceID();
            }
            return signature;
        }

        /// <summary>The menu's readable controls in visual order. The serialized selectable
        /// list groups the footer row before the main stack, so the sweep re-sorts by screen
        /// position: rows top to bottom, left to right within a row. Row grouping uses a
        /// QUARTER of a button's world height: the footer buttons sit a few pixels apart in
        /// Y (one row), while the side promo rides only a third of a button below the stack's
        /// Kingdoms entry and must not merge into its row.</summary>
        private static List<Selectable> Swept(MainMenuUiScreenBhv menu) {
            var swept = new List<Selectable>();
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
                swept.Add(selectable);
            }
            swept.Sort(VisualOrder);
            return swept;
        }

        private static int VisualOrder(Selectable a, Selectable b) {
            Vector3 positionA = a.transform.position;
            Vector3 positionB = b.transform.position;
            float rowTolerance = Mathf.Min(WorldHeight(a), WorldHeight(b)) * 0.25f;
            if (Mathf.Abs(positionA.y - positionB.y) > rowTolerance) {
                return positionB.y.CompareTo(positionA.y); // higher on screen first
            }
            return positionA.x.CompareTo(positionB.x); // same row: left to right
        }

        private static float WorldHeight(Selectable selectable) {
            var rect = selectable.transform as RectTransform;
            return rect == null ? 0f : rect.rect.height * rect.lossyScale.y;
        }

        /// <summary>Whether the button the rebuilt tree lands on (the sorted sweep's first)
        /// reads cleanly: interactable (<c>IsInteractable()</c> also covers the CanvasGroup
        /// locks) with its label available. The open animation holds every button disabled
        /// with its tooltip caption off, and the unlock staggers across frames, so readiness
        /// of other buttons proves nothing about the landing.</summary>
        private static bool LandingReady(MainMenuUiScreenBhv menu) {
            var swept = Swept(menu);
            if (swept.Count == 0) {
                return false;
            }
            var first = swept[0];
            return first.IsInteractable()
                && !string.IsNullOrEmpty(UiText.FirstLabel(first.gameObject));
        }

    }
}
