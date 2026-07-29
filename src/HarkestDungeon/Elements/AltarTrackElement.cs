using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// The icon button heading a candle progress track on an altar panel (a Living City hero,
    /// an Intrepid Coast stat): the track's name, with its spent/total candles. Enter is the
    /// game's own spend - one candle into the track (partial progress banks toward the next
    /// milestone) - and reads back the moved total, or "unavailable" when the spend no-ops
    /// (no candles, track full). A track the game has disabled reads "unavailable" from the
    /// disabled button.
    /// </summary>
    public class AltarTrackElement : SelectableElement {
        internal static readonly AccessTools.FieldRef<AltarProgressTrackBaseBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<AltarProgressTrackBaseBhv, DataContextBhv>("m_dataContextBhv");

        private readonly AltarProgressTrackBaseBhv _track;

        public AltarTrackElement(AltarProgressTrackBaseBhv track, Selectable selectable,
            System.Func<string> label) : base(selectable, label) {
            _track = track;
        }

        protected string Total => ContextField(_track).GetStringValue("track_total_spent");

        public override string Value {
            get {
                string total = Total;
                return Selectable != null && !Selectable.interactable
                    ? SpokenLine.Join(total, S.StatusUnavailable)
                    : total;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.interactable) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, Spend);
        }

        protected virtual void Spend() {
            string before = Total;
            _track.OnTrackSpendAttempt();
            SpeakSpendResult(before);
        }

        protected void SpeakSpendResult(string before) {
            string after = Total;
            SpeechPipeline.Instance?.Speak(after != before ? after : S.StatusUnavailable,
                interrupt: true);
        }
    }
}
