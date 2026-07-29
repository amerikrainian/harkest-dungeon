using Assets.Code.Data;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's item-unlock panel (<c>AltarItemSubScreenBhv</c> - "The Working Fields"
    /// spends candles on random item unlocks; not to be confused with "The Recollection",
    /// the browse-only gallery on <see cref="AltarCollectionScreen"/>). Layout: the candle
    /// balance, the total progress line ("Recollection: 0/163"), then the unlock-category
    /// buttons with their progress and cost ("Trinkets, 0/73, 1 candle"). Enter purchases in
    /// one press; the reveal that follows is read by its modal screen, and on return focus
    /// lands back on the purchased category with its updated counts. Also matches the
    /// panel's reroll variant, which replaces it once all items are collected. Escape closes
    /// the panel through its own close flow (a raw stack pop would leave the altar's region
    /// markers disabled).
    /// </summary>
    public sealed class AltarItemScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarItemSubScreenBhv, DataContextBhv> PanelContextField =
            AccessTools.FieldRefAccess<AltarItemSubScreenBhv, DataContextBhv>("m_dataContextBhv");

        private AltarItemSubScreenBhv _panel;
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
            _panel = top == null ? null : top.GetComponent<AltarItemSubScreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarItemSubScreenBhv)target;
            // The panel's own close (the done button's wiring) - a raw stack pop would skip
            // the altar's pop flow and leave every region marker disabled.
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (panel.IsPresenting) {
                    panel.OnTimelineResume();
                } else {
                    panel.CloseSubscreen();
                }
            });

            _root.Add(AltarScreen.CandleBalance());
            _root.Add(new ReadoutElement(() => {
                var context = PanelContextField(panel);
                return context == null ? null : context.GetStringValue("unlock_total_progress");
            }));

            var buttons = new Container(ContainerShape.VerticalList);
            foreach (var button in panel.GetComponentsInChildren<AltarItemRewardButtonBhv>(includeInactive: false)) {
                var selectable = button.GetComponent<Selectable>();
                if (selectable != null) {
                    buttons.Add(new AltarUnlockButtonElement(button,
                        () => panel.IsPresenting, panel.OnTimelineResume, selectable,
                        purchased => _lastPurchase = purchased));
                }
            }
            _root.Add(buttons);

            // Re-entry after a reveal: land back on the category just purchased, so its
            // updated count is the landing line and another Enter pulls again.
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
    }
}
