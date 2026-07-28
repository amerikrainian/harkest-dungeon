using Assets.Code.Kingdom;
using Assets.Code.Kingdom.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The kingdom day/event notification (a <c>ScreenKingdomMapEventPanel</c> widget on a Map
    /// layer stack entry): the day and the event's title, effect, and flavour as the first
    /// element, then any reward items, then the close button. The eventless variant reads the
    /// day alone. Escape closes through the screen's own TryCloseScreen, which the game
    /// swallows during the first stretch of the slow day-turn intro.
    /// </summary>
    public sealed class KingdomEventPanelScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ScreenKingdomMapEventPanel, TextMeshProUGUI> EffectTextField =
            AccessTools.FieldRefAccess<ScreenKingdomMapEventPanel, TextMeshProUGUI>("m_effectDescriptionText");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapEventPanel, TextMeshProUGUI> FlavourTextField =
            AccessTools.FieldRefAccess<ScreenKingdomMapEventPanel, TextMeshProUGUI>("m_flavourText");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapEventPanel, KingdomEventInstance> EventField =
            AccessTools.FieldRefAccess<ScreenKingdomMapEventPanel, KingdomEventInstance>("m_kingdomEventInstance");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapEventPanel, Button> CloseButtonField =
            AccessTools.FieldRefAccess<ScreenKingdomMapEventPanel, Button>("m_closeButton");

        private ScreenKingdomMapEventPanel _panel;
        private Container _root;
        private int _builtSignature;

        public override string Name {
            get {
                var context = _panel == null ? null : _panel.GetComponent<Assets.Code.Data.DataContextBhv>();
                string day = context == null ? null : context.GetStringValue("day");
                return string.IsNullOrEmpty(day) ? S.ScreenKingdomEvent : day;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponentInChildren<ScreenKingdomMapEventPanel>(false);
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (ScreenKingdomMapEventPanel)target;
            var screen = panel.GetComponentInParent<UiScreenBhv>();
            _root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (ScreenKingdomMapEventPanel)target;
            if (Signature(panel) != _builtSignature) {
                _root.Clear();
                Populate(panel);
                return true;
            }
            return false;
        }

        private void Populate(ScreenKingdomMapEventPanel panel) {
            var context = panel.GetComponent<Assets.Code.Data.DataContextBhv>();
            _root.Add(new ReadoutElement(() => {
                string day = context == null ? null : context.GetStringValue("day");
                if (EventField(panel) == null) {
                    return day;
                }
                string title = context == null ? null : context.GetStringValue("title_label");
                // The typewriter reveals the flavour gradually; the full text is set at open.
                var effect = EffectTextField(panel);
                var flavour = FlavourTextField(panel);
                return SpokenLine.Join(day, title,
                    effect == null ? null : effect.text,
                    flavour == null ? null : flavour.text);
            }));
            foreach (var reward in panel.GetComponentsInChildren<Assets.Code.UI.Items.UninteractableRewardItemBhv>(includeInactive: false)) {
                var captured = reward;
                _root.Add(new ReadoutElement(
                    () => UiText.AllText(captured.gameObject),
                    detail: () => TooltipReader.Lines(captured.gameObject)));
            }
            var close = CloseButtonField(panel);
            if (close != null && close.gameObject.activeInHierarchy) {
                _root.Add(new SelectableElement(close));
            }
            _builtSignature = Signature(panel);
        }

        private static int Signature(ScreenKingdomMapEventPanel panel) {
            int signature = 17;
            var evt = EventField(panel);
            signature = signature * 31 + (evt == null ? 0 : evt.GetHashCode());
            foreach (var reward in panel.GetComponentsInChildren<Assets.Code.UI.Items.UninteractableRewardItemBhv>(includeInactive: false)) {
                signature = signature * 31 + reward.GetInstanceID();
            }
            return signature;
        }
    }
}
