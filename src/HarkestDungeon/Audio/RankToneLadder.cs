using System.Collections.Generic;
using DD2A11y.Core.Audio;

namespace DD2A11y.Audio {
    /// <summary>
    /// Plays a hero's rank-coverage sounds (<see cref="RankTones"/>), the panel's two pip rows
    /// as two voices: the rank tone pitched by skills usable FROM a rank, the harmonizing
    /// target tone by skills able to HIT that enemy rank. A pool hero or path seal plays four
    /// two-voice chords, rank 1 to 4; a party hero plays their own rank's lone rank tone, then
    /// the four target tones - their fit where they stand, followed by their reach. Scheduled
    /// on a focus landing, sounded step by step from the owning screen's per-frame update, so
    /// the sound dies with the screen. A new schedule replaces whatever is still pending - it
    /// describes the focused hero, and focus has moved on.
    /// </summary>
    public sealed class RankToneLadder {
        private struct Step {
            public float Due;
            public bool HasRank;
            public float RankPitch;
            public bool HasTarget;
            public float TargetPitch;
        }

        private readonly IAudioEngine _audio;
        private readonly List<Step> _pending = new List<Step>();

        public RankToneLadder(IAudioEngine audio) {
            _audio = audio;
        }

        /// <summary>Queue the party-hero reading: one rank tone for the rank the hero stands
        /// in (<paramref name="rankCount"/> over <paramref name="limit"/>), then the target
        /// row as a four-tone phrase.</summary>
        public void ScheduleParty(int rankCount, int[] targetCounts, int limit) {
            _pending.Clear();
            float now = UnityEngine.Time.unscaledTime;
            _pending.Add(new Step {
                Due = now,
                HasRank = true,
                RankPitch = RankTones.Pitch(rankCount, limit),
            });
            if (targetCounts == null) {
                return;
            }
            for (int rank = 0; rank < targetCounts.Length; rank++) {
                _pending.Add(new Step {
                    Due = now + (rank + 1) * RankTones.Spacing,
                    HasTarget = true,
                    TargetPitch = RankTones.Pitch(targetCounts[rank], limit),
                });
            }
        }

        /// <summary>Queue the full ladder: one chord per rank, ascending, both voices pitched
        /// by their row's count over <paramref name="limit"/>. Null counts (no class data)
        /// just clears.</summary>
        public void ScheduleLadder(int[] launchCounts, int[] targetCounts, int limit) {
            _pending.Clear();
            if (launchCounts == null) {
                return;
            }
            float now = UnityEngine.Time.unscaledTime;
            for (int rank = 0; rank < launchCounts.Length; rank++) {
                _pending.Add(new Step {
                    Due = now + rank * RankTones.Spacing,
                    HasRank = true,
                    RankPitch = RankTones.Pitch(launchCounts[rank], limit),
                    HasTarget = targetCounts != null,
                    TargetPitch = targetCounts == null
                        ? 1f : RankTones.Pitch(targetCounts[rank], limit),
                });
            }
        }

        public void Clear() => _pending.Clear();

        /// <summary>Play the steps that have come due.</summary>
        public void Tick() {
            if (_pending.Count == 0) {
                return;
            }
            float now = UnityEngine.Time.unscaledTime;
            while (_pending.Count > 0 && _pending[0].Due <= now) {
                var step = _pending[0];
                _pending.RemoveAt(0);
                if (step.HasRank) {
                    _audio.PlayCue(AudioCue.CrossroadsRankTone, 1f, 0f, step.RankPitch);
                }
                if (step.HasTarget) {
                    _audio.PlayCue(AudioCue.CrossroadsTargetTone, 1f, 0f, step.TargetPitch);
                }
            }
        }
    }
}
