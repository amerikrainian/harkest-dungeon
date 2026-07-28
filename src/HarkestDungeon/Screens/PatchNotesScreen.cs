using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The patch notes screen (a Modal-layer stack screen opened from the main menu and pause
    /// menu), named by its own title label: the current page as one element (Left/Right flip
    /// pages, the page's lines in the buffer), then the Close button. The caption-less
    /// prev/next arrow buttons stay out of the tree - paging lives on the page element.
    /// </summary>
    public sealed class PatchNotesScreen : GameScreen {
        private UiScreenBhv _screen;
        private PatchNotesWidgetBhv _widget;
        private Container _root;
        private PatchNotesPageElement _page;
        // The widget writes the page it settles on during the screen's own open step, after our
        // tree is built: until the screen reports Open the label holds the prefab's placeholder
        // ("HOTFIX 0.13.{version}") or, on a reopen, the page left from last time. The page
        // reads as nothing until then, and settling asks for the one re-announce.
        private bool _awaitingText;

        public override string Name {
            get {
                // The title carries no loc key; the first TMP under the screen is the title
                // label, read live so it follows whatever the game shows.
                if (_screen != null) {
                    foreach (var tmp in _screen.GetComponentsInChildren<TMP_Text>(includeInactive: false)) {
                        if (!string.IsNullOrWhiteSpace(tmp.text)) {
                            return tmp.text;
                        }
                    }
                }
                return S.ScreenGeneric;
            }
        }

        public override object ResolveTarget() {
            if (!SingletonMonoBehaviour<ScreenStackBhv>.HasInstance()) {
                _widget = null;
                return null;
            }
            var top = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
            if (top == null || top.m_screenType != ScreenStackBhv.ScreenOrderType.SCREEN || top.m_screenObj == null) {
                _widget = null;
                return null;
            }
            _widget = top.m_screenObj.GetComponentInChildren<PatchNotesWidgetBhv>(includeInactive: false);
            if (_widget == null) {
                return null;
            }
            _screen = top.m_screenObj.GetComponent<UiScreenBhv>();
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (PatchNotesWidgetBhv)target;
            var screen = _screen;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            _page = new PatchNotesPageElement(widget, () => screen.ScreenState == UiScreenState.Open);
            _root.Add(_page);
            _awaitingText = screen.ScreenState != UiScreenState.Open;
            foreach (var selectable in screen.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable is Scrollbar || !UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue; // the scroll plumbing and the caption-less page-arrow buttons
                }
                _root.Add(new SelectableElement(selectable));
            }
            return _root;
        }

        // One re-announce when the screen settles and the real page becomes readable. Only on
        // entry: a page flip changes the text too, and the navigator's own adjust speaks that.
        public override bool OnUpdate(object target) {
            if (!_awaitingText || _screen.ScreenState != UiScreenState.Open) {
                return false;
            }
            _awaitingText = false;
            return true;
        }
    }
}
