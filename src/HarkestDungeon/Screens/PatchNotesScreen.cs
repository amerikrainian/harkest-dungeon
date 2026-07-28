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
    /// menu), named by its own title label: the page header first, then that page's notes one
    /// row each, then the Close button. Left/Right flip pages from anywhere on the screen -
    /// a page runs to dozens of notes, so paging must not mean arrowing back to the top - and
    /// land on the new page's header, the one place a flip can leave focus honestly (every
    /// note below it belongs to a page that is now gone). The caption-less prev/next arrow
    /// buttons stay out of the tree; the notes read as rows instead.
    /// </summary>
    public sealed class PatchNotesScreen : GameScreen {
        private readonly TraditionalNavigator _navigator;
        private UiScreenBhv _screen;
        private PatchNotesWidgetBhv _widget;
        private Container _root;
        private PatchNotesPageElement _page;
        private Container _notes;
        private string _builtText;
        // The widget writes the page it settles on during the screen's own open step, after our
        // tree is built: until the screen reports Open the label holds the prefab's placeholder
        // ("HOTFIX 0.13.{version}") or, on a reopen, the page left from last time. The page
        // reads as nothing until then, and settling asks for the one re-announce.
        private bool _awaitingText;

        public PatchNotesScreen(TraditionalNavigator navigator) => _navigator = navigator;

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
            _notes = new Container(ContainerShape.VerticalList);
            _root.Add(_notes);
            RebuildNotes();
            foreach (var selectable in screen.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable is Scrollbar || !UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue; // the scroll plumbing and the caption-less page-arrow buttons
                }
                _root.Add(new SelectableElement(selectable));
            }
            return _root;
        }

        // One row per note on the current page, the header line excluded (the page selector
        // above already reads it). Rows read their line live by index, so they always speak the
        // page on screen even in the frame before a flip rebuilds them.
        private void RebuildNotes() {
            _builtText = _page.PageText();
            _notes.Clear();
            int count = 0;
            while (_page.Line(count + 1) != null) {
                count++;
            }
            for (int i = 1; i <= count; i++) {
                int index = i;
                _notes.Add(new ReadoutElement(() => _page.Line(index)));
            }
        }

        // Left/Right belong to the whole screen, not just the header row: flip, rebuild the
        // notes, and land focus on the new header (speaking it). A refused flip at either end
        // re-reads the header, the same answer the navigator's own adjust gives.
        public override bool HandleAction(string actionKey) {
            if (actionKey != UiActions.Left && actionKey != UiActions.Right) {
                return false;
            }
            if (actionKey == UiActions.Right) {
                _widget.TryNextPage();
            } else {
                _widget.TryPreviousPage();
            }
            RebuildNotes();
            _navigator.Focus(_page, announce: true);
            return true;
        }

        public override bool OnUpdate(object target) {
            // A page flip (and the settle that first fills the page) swaps every note; focus
            // sits on the page selector outside this container, so it survives the rebuild.
            if (_page.PageText() != _builtText) {
                RebuildNotes();
            }
            // One re-announce when the screen settles and the real page becomes readable. Only
            // on entry: a flip changes the text too, and the navigator's own adjust speaks that.
            if (!_awaitingText || _screen.ScreenState != UiScreenState.Open) {
                return false;
            }
            _awaitingText = false;
            return true;
        }
    }
}
