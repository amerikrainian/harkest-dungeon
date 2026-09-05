using Assets.Code.UI.Controllers;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>The pause menu: its buttons in the game's own navigation order, then the profile
    /// badge - a live button whose only visible content is the profile's level art, wired to
    /// the game's profile summary - named for the screen it opens. Escape returns to the game
    /// via the menu's own Return button logic.</summary>
    public sealed class PauseScreen : GameScreen {
        private const string ProfileHandler = "OnProfileButtonPress";
        private static bool _badgeWarned;
        private static readonly AccessTools.FieldRef<PauseMenuUiControllerBhv, Selectable[]> OrderedField =
            AccessTools.FieldRefAccess<PauseMenuUiControllerBhv, Selectable[]>("m_orderedSelectableNavigationList");

        private PauseMenuUiControllerBhv _pause;
        private Container _root;
        private int _builtCount;

        public override string Name => S.ScreenPauseMenu;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _pause = top == null ? null : top.GetComponentInChildren<PauseMenuUiControllerBhv>(includeInactive: false);
            return _pause;
        }

        public override Container BuildRoot(object target) {
            var pause = (PauseMenuUiControllerBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: pause.ButtonReturn);
            Populate(pause);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var pause = (PauseMenuUiControllerBhv)target;
            if (CountActive(pause) != _builtCount) {
                _root.Clear();
                Populate(pause);
            }
            return LabelArrived(_root, ref _awaitingLabel);
        }

        /// <summary>Screens entered mid-open-animation land on a control whose caption has not
        /// been written yet; once the first focusable gains its label, request one re-announce so
        /// the player is not left with a bare "button".</summary>
        internal static bool LabelArrived(Container root, ref bool awaiting) {
            if (!awaiting) {
                return false;
            }
            var first = root.FirstFocusable();
            if (first != null && !string.IsNullOrEmpty(first.Label)) {
                awaiting = false;
                return true;
            }
            return false;
        }

        private bool _awaitingLabel;

        private void Populate(PauseMenuUiControllerBhv pause) {
            int count = 0;
            foreach (var selectable in OrderedField(pause)) {
                if (selectable == null || !selectable.gameObject.activeInHierarchy
                    || !Game.UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue;
                }
                _root.Add(new SelectableElement(selectable));
                count++;
            }
            var badge = ProfileBadge(pause);
            if (badge != null) {
                _root.Add(new SelectableElement(badge, () => Game.GameLoc.TryGet("profile_summary_title_label")));
            }
            _builtCount = count;
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
        }

        // The badge carries no text source of its own (the text sweep drops it), so it is
        // known by the handler its click is wired to.
        private static Button ProfileBadge(PauseMenuUiControllerBhv pause) {
            foreach (var button in pause.GetComponentsInChildren<Button>(includeInactive: false)) {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) {
                    if (button.onClick.GetPersistentMethodName(i) == ProfileHandler) {
                        return button;
                    }
                }
            }
            if (!_badgeWarned) {
                _badgeWarned = true;
                Plugin.Log.LogWarning("PauseScreen: no button wired to " + ProfileHandler + "; the profile summary is unreachable");
            }
            return null;
        }

        private static int CountActive(PauseMenuUiControllerBhv pause) {
            int count = 0;
            foreach (var selectable in OrderedField(pause)) {
                if (selectable != null && selectable.gameObject.activeInHierarchy
                    && Game.UiText.HasAnyTextSource(selectable.gameObject)) {
                    count++;
                }
            }
            return count;
        }
    }
}
