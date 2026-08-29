namespace DD2A11y.Core.Audio {
    /// <summary>
    /// The skills-per-rank ladder: four <see cref="AudioCue.CrossroadsRankTone"/> one-shots,
    /// rank 1 to 4, each pitched by how many of the hero's equipped skills act from that rank.
    /// The pitch spans one octave from the authored tone (no skills) up to its double (a full
    /// loadout), so each extra skill steps the tone up audibly and the four-tone contour reads
    /// as the hero's positional profile.
    /// </summary>
    public static class RankTones {
        /// <summary>Seconds between one ladder tone's start and the next.</summary>
        public const float Spacing = 0.13f;

        /// <summary>The playback rate for a rank covered by <paramref name="count"/> of the
        /// hero's <paramref name="limit"/> equippable skills: 1 at zero, 2 (an octave up) at
        /// the full limit.</summary>
        public static float Pitch(int count, int limit) {
            if (limit <= 0) {
                return 1f;
            }
            int clamped = count < 0 ? 0 : count > limit ? limit : count;
            return (float)System.Math.Pow(2, clamped / (double)limit);
        }
    }
}
