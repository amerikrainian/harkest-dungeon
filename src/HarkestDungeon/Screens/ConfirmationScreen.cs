using Assets.Code.Data;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// The game's shared confirmation dialog ("Are you sure you'd like to quit?"): the title and
    /// body as the first focusable line, then each live choice, all on Up/Down. Escape declines.
    /// Text comes from the dialog's DataContext, where Init stored the already-localized strings.
    /// </summary>
    public sealed class ConfirmationScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ConfirmationDialogBhv, GameObject> AcceptField =
            AccessTools.FieldRefAccess<ConfirmationDialogBhv, GameObject>("m_AcceptBtn");
        private static readonly AccessTools.FieldRef<ConfirmationDialogBhv, GameObject> DeclineField =
            AccessTools.FieldRefAccess<ConfirmationDialogBhv, GameObject>("m_DeclineBtn");

        private ConfirmationDialogBhv _dialog;

        public override string Name => S.ScreenDialog;

        public override object ResolveTarget() {
            var top = StackTop.Raw();
            _dialog = top == null ? null : top.GetComponentInChildren<ConfirmationDialogBhv>(includeInactive: false);
            return _dialog;
        }

        public override Container BuildRoot(object target) {
            var dialog = (ConfirmationDialogBhv)target;
            var context = dialog.GetComponent<DataContextBhv>();
            bool hasDecline = DeclineField(dialog) != null && DeclineField(dialog).activeSelf;

            var root = new RootContainer(ContainerShape.VerticalList,
                back: hasDecline ? dialog.OnDeclinePressed : (System.Action)null);

            root.Add(new StaticTextElement(() => SpokenLine.Join(". ",
                new[] { context.GetStringValue("confirmation_title"), context.GetStringValue("confirmation_desc") })));

            var accept = AcceptField(dialog);
            if (accept != null && accept.activeSelf) {
                root.Add(new ActionElement(
                    () => FirstNonEmpty(context.GetStringValue("confirmation_label"), UiText.FirstLabel(accept)),
                    S.RoleButton, dialog.OnConfirmPressed));
            }
            if (hasDecline) {
                var decline = DeclineField(dialog);
                root.Add(new ActionElement(
                    () => FirstNonEmpty(context.GetStringValue("decline_label"), UiText.FirstLabel(decline)),
                    S.RoleButton, dialog.OnDeclinePressed));
            }
            return root;
        }

        private static string FirstNonEmpty(string a, string b) => string.IsNullOrEmpty(a) ? b : a;
    }
}
