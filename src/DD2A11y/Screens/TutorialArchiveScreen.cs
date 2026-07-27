using System.Collections.Generic;
using Assets.Code.Tutorial;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The tutorial archive (a stack entry opened from the pause menu, or pushed by the game
    /// with a specific tutorial), named by its own "Archive" title. One element per tutorial
    /// in the game's own list order - majors with their category's minors after them - each
    /// reading its title, prefixed "New" while the game shows its unviewed notification icon.
    /// Enter is the game's own click: the entry's text opens in the side panel and is spoken
    /// in full, then stays reviewable line by line in the buffer; the game marks the entry
    /// viewed and saves. Escape closes through the screen's own teardown.
    /// </summary>
    public sealed class TutorialArchiveScreen : GameScreen {
        private TutorialArchiveWidgetBhv _widget;
        private Container _root;
        private int _builtSignature;

        public override string Name => GameLoc.TryGet("tutorial_menu_title") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<TutorialArchiveWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (TutorialArchiveWidgetBhv)target;
            var screen = widget.GetComponentInParent<UiScreenBhv>();
            _root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            Populate(widget);
            return _root;
        }

        // The router matches the pushed screen before its own OnScreenOpenStart has spawned
        // the option rows, so the list fills (and can grow by one when the game pushes an
        // unlisted tutorial) after our entry - rebuild whenever the swept set changes.
        public override bool OnUpdate(object target) {
            var widget = (TutorialArchiveWidgetBhv)target;
            if (Signature(widget) != _builtSignature) {
                _root.Clear();
                Populate(widget);
                return true;
            }
            return false;
        }

        private void Populate(TutorialArchiveWidgetBhv widget) {
            foreach (var option in Options(widget)) {
                _root.Add(new TutorialOptionElement(widget, option, option.GetComponent<Selectable>()));
            }
            _builtSignature = Signature(widget);
        }

        // The inactive template row never appears in the sweep, but the frame between the
        // push and the game's populate can still surface it - the type check keeps any
        // uninitialized row out.
        private static IEnumerable<TutorialArchiveOptionBhv> Options(TutorialArchiveWidgetBhv widget) {
            foreach (var option in widget.GetComponentsInChildren<TutorialArchiveOptionBhv>(includeInactive: false)) {
                if (TutorialOptionElement.TypeOf(option) != null && option.GetComponent<Selectable>() != null) {
                    yield return option;
                }
            }
        }

        private static int Signature(TutorialArchiveWidgetBhv widget) {
            int signature = 17;
            foreach (var option in Options(widget)) {
                signature = signature * 31 + option.GetInstanceID();
            }
            return signature;
        }
    }
}
