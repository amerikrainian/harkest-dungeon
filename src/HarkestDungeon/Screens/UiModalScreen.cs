using Assets.Code.Data;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>The generic Ok / YesNo / YesNoCancel modal: title and body first, then each
    /// instantiated button. Buttons carry their own localized captions.</summary>
    public sealed class UiModalScreen : GameScreen {
        private UiModalBhv _modal;

        public override string Name => S.ScreenDialog;

        public override object ResolveTarget() {
            var top = StackTop.Raw();
            _modal = top == null ? null : top.GetComponent<UiModalBhv>();
            return _modal;
        }

        public override Container BuildRoot(object target) {
            var modal = (UiModalBhv)target;
            var context = modal.GetComponent<DataContextBhv>();
            var root = new RootContainer(ContainerShape.VerticalList);

            root.Add(new StaticTextElement(() => SpokenLine.Join(". ",
                new[] { context.GetStringValue("title_text"), context.GetStringValue("body_text") })));

            foreach (var button in modal.GetComponentsInChildren<Button>(includeInactive: false)) {
                root.Add(new SelectableElement(button));
            }
            return root;
        }
    }
}
