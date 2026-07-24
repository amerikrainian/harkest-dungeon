using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The floor under every stack screen the mod has no dedicated reader for (glossary, road
    /// node panels): a generic sweep of the screen's selectables so no
    /// surface is ever dead air. Registered last, so dedicated screens always win. Only real
    /// SCREEN stack entries are taken - driving HUD widgets (minimap, goals) register on the
    /// stack too and must not capture the keyboard mid-drive.
    /// </summary>
    public sealed class GenericScreen : GameScreen {
        private UiScreenBhv _screen;
        private Container _root;
        private int _builtCount;
        private bool _awaitingLabel;

        public override string Name {
            get {
                string title = UiText.FirstLabel(_screen != null ? _screen.gameObject : null);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            if (!SingletonMonoBehaviour<ScreenStackBhv>.HasInstance()) {
                return null;
            }
            var top = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
            if (top == null || top.m_screenType != ScreenStackBhv.ScreenOrderType.SCREEN || top.m_screenObj == null) {
                return null;
            }
            _screen = top.m_screenObj.GetComponent<UiScreenBhv>();
            return _screen;
        }

        public override Container BuildRoot(object target) {
            var screen = (UiScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            Populate(screen);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var screen = (UiScreenBhv)target;
            if (CountActive(screen) != _builtCount) {
                _root.Clear();
                Populate(screen);
            }
            return PauseScreen.LabelArrived(_root, ref _awaitingLabel);
        }

        private void Populate(UiScreenBhv screen) {
            int count = 0;
            foreach (var selectable in screen.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (!Include(selectable)) {
                    continue;
                }
                _root.Add(new SelectableElement(selectable));
                count++;
            }
            _builtCount = count;
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
        }

        private static int CountActive(UiScreenBhv screen) {
            int count = 0;
            foreach (var selectable in screen.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (Include(selectable)) {
                    count++;
                }
            }
            return count;
        }

        private static bool Include(Selectable selectable) {
            if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                return false;
            }
            return UiText.HasAnyTextSource(selectable.gameObject);
        }

    }
}
