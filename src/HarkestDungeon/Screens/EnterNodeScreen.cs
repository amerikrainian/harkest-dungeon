using System;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The road's node-arrival prompt (<c>EnterNodeScreenWidgetBhv</c>): every roadside stop
    /// halts the coach on this one screen - a single button naming the interaction ("Search
    /// the Cache", "The Field Hospital"), with the candle marker spoken when entering also
    /// feeds a hero goal (the game shows an icon only). The label reads through the push
    /// params' own loc key, so the entry never races the widget's late text bind. Enter is
    /// the button's own press; the game refuses to close the prompt, so Escape answers
    /// unavailable.
    /// </summary>
    public sealed class EnterNodeScreen : GameScreen {
        private readonly Action<string, bool> _speak;
        private EnterNodeScreenWidgetBhv _widget;
        private Container _root;
        private bool _awaitingLabel;

        public EnterNodeScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name => S.ScreenNodePrompt;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<EnterNodeScreenWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (EnterNodeScreenWidgetBhv)target;
            var screen = widget.GetComponentInParent<UiScreenBhv>();
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => _speak(S.StatusUnavailable, true));
            _root.Add(new ActionElement(
                () => {
                    var pushParams = Params(screen);
                    return pushParams == null ? null : GameLoc.TryGet(pushParams.m_buttonString);
                },
                S.RoleButton,
                widget.OnInteractButton,
                value: () => Params(screen)?.m_hasLootCandle == true ? S.NodeCandleReward : null));
            // A fresh prompt's push params land a frame after the object tops the stack; the
            // arrival of the button's label requests the one re-announce.
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
            return _root;
        }

        public override bool OnUpdate(object target) => PauseScreen.LabelArrived(_root, ref _awaitingLabel);

        private static EnterNodeScreenPushParams Params(UiScreenBhv screen) =>
            screen == null ? null : screen.PushParams as EnterNodeScreenPushParams;
    }
}
