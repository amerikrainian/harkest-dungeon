using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// The icon button heading a candle progress track on an altar panel (a Living City hero,
    /// an Intrepid Coast stat): the track's name, with its spent/total candles. Enter is the
    /// game's own spend - one candle into the track (partial progress banks toward the next
    /// milestone) - and reads back the moved total plus the next unlock's title and remaining
    /// candle cost, or "unavailable" when the spend no-ops (no candles, track full). A track
    /// the game has disabled reads "unavailable" from the disabled button.
    /// </summary>
    public class AltarTrackElement : SelectableElement {
        internal static readonly AccessTools.FieldRef<AltarProgressTrackBaseBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<AltarProgressTrackBaseBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<AltarProgressTrackBaseBhv, Assets.Code.Unlock.UnlockTrackDefinition> DefinitionField =
            AccessTools.FieldRefAccess<AltarProgressTrackBaseBhv, Assets.Code.Unlock.UnlockTrackDefinition>("m_unlockTrackDefinition");
        private static readonly AccessTools.FieldRef<AltarProgressTrackBaseBhv, ProgressTrackMilestoneBhv> NextMilestoneField =
            AccessTools.FieldRefAccess<AltarProgressTrackBaseBhv, ProgressTrackMilestoneBhv>("m_nextMilestoneUpgrade");

        private readonly AltarProgressTrackBaseBhv _track;

        public AltarTrackElement(AltarProgressTrackBaseBhv track, Selectable selectable,
            System.Func<string> label) : base(selectable, label) {
            _track = track;
        }

        protected string Total => ContextField(_track).GetStringValue("track_total_spent");

        /// <summary>The game's display name for a track: the loc string of its unlock-track
        /// id ("altar_upgrade_memory" is "Memory") - the same key family the stat panel binds.</summary>
        internal static string TrackName(AltarProgressTrackBaseBhv track) {
            var definition = DefinitionField(track);
            return definition == null ? null : Game.GameLoc.TryGet("altar_upgrade_" + definition.m_Id);
        }

        public override string Value => Total;

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
            SpeechPipeline.Instance?.Speak(
                after != before ? Core.Text.SpokenLine.Join(after, NextUnlockLine()) : S.StatusUnavailable,
                interrupt: true);
        }

        /// <summary>The next milestone's title and remaining candle count - what the spend
        /// just cheapened. Null once the track is full (the game then leaves its next-milestone
        /// field on the last, complete milestone).</summary>
        private string NextUnlockLine() {
            var next = NextMilestoneField(_track);
            if (next == null) {
                return null;
            }
            int remaining = AltarMilestoneElement.RemainingCandles(next);
            return remaining <= 0 ? null : Core.Text.SpokenLine.Join(
                AltarMilestoneElement.UnlockTitle(next), S.AltarCandleCost(remaining));
        }
    }
}
