using Assets.Code.UI;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The confession (boss) select screen, pushed onto the Story layer by a road trigger
    /// early in a run, named by its own title: one element per confession option, then the
    /// confirm button (icon-only in the game; captioned with the game's own continue string).
    /// Enter on an option marks it through the game's own submit and arms the confirm; the
    /// confirm press commits the confession and the drive resumes. Escape is deliberately
    /// inert - the choice is mandatory, and a run that gets past this screen without a
    /// confession has no Mountain route and dead-ends at the last inn.
    /// </summary>
    public sealed class BossSelectScreen : GameScreen {
        private static readonly AccessTools.FieldRef<BossSelectWidgetBhv, Button> ContinueField =
            AccessTools.FieldRefAccess<BossSelectWidgetBhv, Button>("m_continueButton");

        private BossSelectWidgetBhv _widget;
        private Container _root;

        public override string Name {
            get {
                string title = _widget == null ? null : UiText.FirstLabel(_widget.gameObject);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<BossSelectWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (BossSelectWidgetBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList);
            foreach (var option in widget.GetComponentsInChildren<SelectBossOptionBhv>(includeInactive: false)) {
                var selectable = option.GetComponent<Selectable>();
                if (selectable != null) {
                    _root.Add(new BossOptionElement(widget, option, selectable));
                }
            }
            var confirm = ContinueField(widget);
            if (confirm != null) {
                _root.Add(new SelectableElement(confirm, () => GameLoc.TryGet("continue_label")));
            }
            return _root;
        }
    }
}
