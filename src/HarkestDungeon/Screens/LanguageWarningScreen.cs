using Assets.Code.Data;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The language disclaimer the game shows on switching to a language it marks as
    /// early-access (a plain <c>UiScreenBhv</c> the settings language dropdown pushes, known
    /// by its bound "language_warning_title"): its title and body as the first element, then
    /// its buttons. Escape closes through the screen's own teardown. The floor read only the
    /// buttons, never the warning itself.
    /// </summary>
    public sealed class LanguageWarningScreen : GameScreen {
        private const string TitleKey = "language_warning_title";

        private UiScreenBhv _screen;
        private Container _root;

        public override string Name => S.ScreenDialog;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            var screen = top == null ? null : top.GetComponent<UiScreenBhv>();
            var context = screen == null ? null : screen.GetComponent<DataContextBhv>();
            _screen = context != null && !string.IsNullOrEmpty(context.GetStringValue(TitleKey)) ? screen : null;
            return _screen;
        }

        public override Container BuildRoot(object target) {
            var screen = (UiScreenBhv)target;
            var context = screen.GetComponent<DataContextBhv>();
            var scroll = screen.GetComponentInChildren<ScrollRect>(includeInactive: false);
            _root = new RootContainer(ContainerShape.VerticalList, back: screen.TryCloseScreen);
            _root.Add(new StaticTextElement(() => {
                string title = context == null ? null : context.GetStringValue(TitleKey);
                string body = scroll == null ? null : UiText.AllText(scroll.gameObject);
                return Core.Text.SpokenLine.Join(". ", new[] { title, body });
            }));
            foreach (var button in screen.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    _root.Add(new SelectableElement(button));
                }
            }
            return _root;
        }
    }
}
