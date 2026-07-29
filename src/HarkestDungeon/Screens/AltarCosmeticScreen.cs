using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's cosmetic panel (<c>AltarCosmeticSubScreenBhv</c> - "The Mountain",
    /// unlocked once every hero is): the candle balance, then one reward button per hero -
    /// named by the hero's class string over the track id the game keys the button to, since
    /// the sighted button is a bare portrait - with the unlock progress and candle cost.
    /// Enter purchases a random weapon kit or palette in one press (the mouse holds the
    /// button); the reveal that follows is read by its modal screen, and on return focus
    /// lands back on the purchased hero. Escape closes through the panel's own flow (a raw
    /// stack pop would leave the altar's region markers disabled).
    /// </summary>
    public sealed class AltarCosmeticScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarItemRewardButtonBhv, string> TrackIdField =
            AccessTools.FieldRefAccess<AltarItemRewardButtonBhv, string>("m_unlockTrackID");

        private AltarCosmeticSubScreenBhv _panel;
        private Container _root;
        private AltarItemRewardButtonBhv _lastPurchase;

        public override string Name {
            get {
                string title = UiText.ChildLabel(_panel != null ? _panel.gameObject : null, "exit_anchor");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<AltarCosmeticSubScreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarCosmeticSubScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (panel.IsPresenting) {
                    panel.OnTimelineResume();
                } else {
                    panel.CloseSubscreen();
                }
            });

            _root.Add(AltarScreen.CandleBalance());

            var buttons = new Container(ContainerShape.VerticalList);
            foreach (var button in panel.GetComponentsInChildren<AltarItemRewardButtonBhv>(includeInactive: false)) {
                var selectable = button.GetComponent<Selectable>();
                if (selectable != null) {
                    buttons.Add(new AltarUnlockButtonElement(button,
                        () => panel.IsPresenting, panel.OnTimelineResume, selectable,
                        purchased => _lastPurchase = purchased,
                        () => HeroName(button)));
                }
            }
            _root.Add(buttons);

            // Re-entry after a reveal: land back on the hero just purchased, so its updated
            // cost is the landing line and another Enter pulls again.
            if (_lastPurchase != null) {
                foreach (var child in buttons.Children) {
                    if (child is AltarUnlockButtonElement element && element.Button == _lastPurchase) {
                        _root.SetFocusedChild(buttons);
                        buttons.SetFocusedChild(child);
                        break;
                    }
                }
            }
            return _root;
        }

        /// <summary>The hero the button rolls cosmetics for: the class string of the unlock
        /// track the game keys it to (the sighted button is a portrait); any label the widget
        /// carries itself is the fallback.</summary>
        private static string HeroName(AltarItemRewardButtonBhv button) {
            string trackId = TrackIdField(button);
            string name = string.IsNullOrEmpty(trackId) ? null : GameLoc.TryGet(trackId);
            return string.IsNullOrEmpty(name) ? UiText.FirstLabel(button.gameObject) : name;
        }
    }
}
