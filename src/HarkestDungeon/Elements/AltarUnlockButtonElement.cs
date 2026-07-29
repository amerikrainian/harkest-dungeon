using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One unlock-category button on an altar reward panel - the recollection's item pools
    /// ("Trinkets, 0/73, 1 candle") or the cosmetic altar's hero buttons: the game's own
    /// label, with the unlock progress and candle cost from the button's data bindings on
    /// the focus line. Enter runs the game's purchase in one press (the mouse holds the
    /// button instead; the purchase validates itself and no-ops when unaffordable or
    /// exhausted, which answers "unavailable"). A DLC-locked cosmetic button reads
    /// "unavailable" with the game's caption in the buffer, and Enter raises the game's own
    /// store dialog. The reveal that follows a purchase is spoken by the reveal screen;
    /// while it presents, Enter continues past it, the game's own Submit behavior.
    /// </summary>
    public sealed class AltarUnlockButtonElement : SelectableElement {
        private static readonly System.Reflection.MethodInfo PurchaseMethod =
            AccessTools.Method(typeof(AltarItemRewardButtonBhv), "Purchase");
        private static readonly System.Reflection.MethodInfo SubmitUpMethod =
            AccessTools.Method(typeof(AltarItemRewardButtonBhv), "SubmitUp");
        private static readonly AccessTools.FieldRef<AltarItemRewardButtonBhv, bool> LockedField =
            AccessTools.FieldRefAccess<AltarItemRewardButtonBhv, bool>("m_isLocked");

        private readonly AltarItemRewardButtonBhv _button;
        private readonly System.Func<bool> _isPresenting;
        private readonly System.Action _resume;
        private readonly System.Action<AltarItemRewardButtonBhv> _onPurchased;

        public AltarUnlockButtonElement(AltarItemRewardButtonBhv button,
            System.Func<bool> isPresenting, System.Action resume, Selectable selectable,
            System.Action<AltarItemRewardButtonBhv> onPurchased, System.Func<string> label = null)
            : base(selectable, label) {
            _button = button;
            _isPresenting = isPresenting;
            _resume = resume;
            _onPurchased = onPurchased;
        }

        /// <summary>The live category widget, for restoring focus after a reveal.</summary>
        public AltarItemRewardButtonBhv Button => _button;

        public override string Value {
            get {
                var context = _button == null ? null : _button.GetComponent<DataContextBhv>();
                if (context == null) {
                    return null;
                }
                if (LockedField(_button)) {
                    return S.StatusUnavailable;
                }
                string progress = context.GetStringValue("unlock_progress");
                string cost = context.GetStringValue("cost_value");
                int candles;
                return SpokenLine.Join(progress,
                    int.TryParse(cost, out candles) ? S.AltarCandleCost(candles) : null);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, () => {
                if (_isPresenting()) {
                    _resume();
                    return;
                }
                if (LockedField(_button)) {
                    // The game's own release handler raises its DLC store dialog.
                    SubmitUpMethod.Invoke(_button, null);
                    return;
                }
                PurchaseMethod.Invoke(_button, null);
                // A landed purchase starts presenting synchronously; anything else was a
                // validated no-op (candles short, category exhausted).
                if (_isPresenting()) {
                    _onPurchased?.Invoke(_button);
                } else {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                }
            });
        }
    }
}
