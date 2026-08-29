namespace DD2A11y.Core.Audio {
    /// <summary>
    /// A named one-shot cue (the engine owns the sound file behind each name, so Core stays free
    /// of paths). Cues carry what speech would lose while driving keys are held: the steerable
    /// pickups and the coach's own motion. One cue per file under assets/audio; placeholders are
    /// replaced 1:1 by dropping in a file with the same name.
    /// </summary>
    public enum AudioCue {
        // The road (assets/audio/road).
        /// <summary>A roadside pickup in sensing range (the repeating positional ping).</summary>
        RoadPickup,
        /// <summary>Drifting off the road's edge.</summary>
        RoadEdgeBump,
        /// <summary>The coach is turning (loops while the turn lasts, panned toward it).</summary>
        RoadTurning,
        /// <summary>The turn ended; the coach runs straight again.</summary>
        RoadTurnEnd,

        // Combat (assets/audio/combat).
        /// <summary>Focus landed on a valid target for the chosen skill (660 Hz).</summary>
        CombatTargetValid,
        /// <summary>Focus landed on an invalid target for the chosen skill (440 Hz).</summary>
        CombatTargetInvalid,
        /// <summary>Focus landed on a skill that telegraphs an affinity change (the A glance
        /// reads the changes themselves).</summary>
        CombatAffinitySkill,

        // Crossroads (assets/audio/crossroads).
        /// <summary>One rank of the focused hero's skills-per-rank ladder, pitched by how many
        /// equipped skills act from that rank (a party hero sounds only their own rank; a pool
        /// hero or path seal sounds all four, rank 1 to 4).</summary>
        CrossroadsRankTone,
        /// <summary>The rank tone's harmony voice in the full ladder: pitched by how many
        /// equipped skills can hit that enemy rank (the panel's Target pip row).</summary>
        CrossroadsTargetTone,
    }

    /// <summary>The glossary's grouping of cues, mirroring the assets/audio folders.</summary>
    public enum AudioCueGroup {
        Road,
        Combat,
        Crossroads,
    }

    public static class AudioCues {
        /// <summary>A cue's group, derived from the enum's naming groups so a new cue lands
        /// in its glossary tab by name alone.</summary>
        public static AudioCueGroup GroupOf(AudioCue cue) {
            string name = cue.ToString();
            if (name.StartsWith("Combat", System.StringComparison.Ordinal)) {
                return AudioCueGroup.Combat;
            }
            if (name.StartsWith("Crossroads", System.StringComparison.Ordinal)) {
                return AudioCueGroup.Crossroads;
            }
            return AudioCueGroup.Road;
        }
    }
}
