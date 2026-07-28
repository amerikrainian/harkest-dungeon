using Assets.Code.UI;
using Assets.Code.UI.Widgets;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One confession option on the boss select screen: the game's own option label (a locked
    /// confession's "???" placeholder reads as unknown), "selected" on the widget's current
    /// choice. Enter is the game's own submit, which marks the choice and arms the confirm
    /// button; the confession is only committed there.
    /// </summary>
    public sealed class BossOptionElement : SelectableElement {
        private static readonly AccessTools.FieldRef<BossSelectWidgetBhv, SelectBossOptionBhv> SelectedField =
            AccessTools.FieldRefAccess<BossSelectWidgetBhv, SelectBossOptionBhv>("m_selectedOption");

        private readonly BossSelectWidgetBhv _widget;
        private readonly SelectBossOptionBhv _option;

        public BossOptionElement(BossSelectWidgetBhv widget, SelectBossOptionBhv option, Selectable selectable)
            : base(selectable) {
            _widget = widget;
            _option = option;
        }

        public override string Value => SelectedField(_widget) == _option ? S.StatusSelected : base.Value;

        public override bool ReannounceOnActivate => true;
    }
}
