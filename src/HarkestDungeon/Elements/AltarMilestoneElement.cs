using System.Collections.Generic;
using Assets.Code.Profile;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One milestone diamond on an altar progress track: the reward title from the diamond's
    /// own tooltip (the base label fallback), with the candles still needed from the track's
    /// current progress - the same number the sighted tooltip shows - or "unlocked" once
    /// bought. Enter buys everything up to this milestone in one press, the purchase the
    /// mouse holds the diamond for; the game gates that hold on affording the whole jump, so
    /// short-or-bought answers "unavailable" (partial investing is the hero button's job).
    /// The reward's description reads from the buffer.
    /// </summary>
    public sealed class AltarMilestoneElement : SelectableElement {
        private static readonly AccessTools.FieldRef<ProgressTrackMilestoneBhv, float> SpentField =
            AccessTools.FieldRefAccess<ProgressTrackMilestoneBhv, float>("m_spentAmount");
        private static readonly AccessTools.FieldRef<ProgressTrackMilestoneBhv, float> GoalField =
            AccessTools.FieldRefAccess<ProgressTrackMilestoneBhv, float>("m_goalAmount");
        private static readonly AccessTools.FieldRef<ProgressTrackMilestoneBhv, Assets.Code.Unlock.UnlockDefinition> UnlockField =
            AccessTools.FieldRefAccess<ProgressTrackMilestoneBhv, Assets.Code.Unlock.UnlockDefinition>("m_unlock");

        private readonly ProgressTrackMilestoneBhv _milestone;

        public AltarMilestoneElement(ProgressTrackMilestoneBhv milestone, Selectable selectable)
            : base(selectable) {
            _milestone = milestone;
        }

        /// <summary>Candles still needed to reach the milestone; zero or less once bought.</summary>
        internal static int RemainingCandles(ProgressTrackMilestoneBhv milestone)
            => (int)(GoalField(milestone) - SpentField(milestone));

        /// <summary>The milestone's reward title, from the loc key the game's own tooltip
        /// heads itself with.</summary>
        internal static string UnlockTitle(ProgressTrackMilestoneBhv milestone)
            => Game.GameLoc.TryGet("upgrade_track_" + UnlockField(milestone).m_Id + "_title");

        private int Remaining => RemainingCandles(_milestone);

        public override string Value => Remaining <= 0 ? S.AltarUnlocked : S.AltarCandleCost(Remaining);

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                int remaining = Remaining;
                if (remaining <= 0 || !SingletonMonoBehaviour<ProfileBhv>.Instance
                        .GetCurrentProfile().CanAffordCandleCost(remaining)) {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                    return;
                }
                _milestone.AttemptToPurchaseMilestone();
                // The purchase updates the whole track synchronously; the game's own guard
                // (a track locked behind its hero's quest) can still no-op it.
                SpeechPipeline.Instance?.Speak(Remaining <= 0 ? S.AltarUnlocked : S.StatusUnavailable,
                    interrupt: true);
            });
        }
    }
}
