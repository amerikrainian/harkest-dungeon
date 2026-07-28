namespace DD2A11y.Core.Audio {
    /// <summary>
    /// A named one-shot cue (the engine owns the sound file behind each name, so Core stays free
    /// of paths). Cues carry what speech would lose while driving keys are held: road events as
    /// they happen, and the identity of each route at a fork. One cue per file under
    /// assets/audio; placeholders are replaced 1:1 by dropping in a file with the same name.
    /// </summary>
    public enum AudioCue {
        // The road (assets/audio/road).
        /// <summary>A roadside pickup in sensing range (the repeating positional ping).</summary>
        RoadPickup,
        /// <summary>A pickup was collected.</summary>
        RoadPickupTaken,
        /// <summary>An ambush event.</summary>
        RoadAmbush,
        /// <summary>A fork is ahead (its route menu follows when the coach stops).</summary>
        RoadFork,
        /// <summary>A blocked route (a barricade fight ahead).</summary>
        RoadBarricade,
        /// <summary>A blocked route cleared.</summary>
        RoadBarricadeOpen,
        /// <summary>Crossed into an event's trigger zone.</summary>
        RoadZoneEnter,
        /// <summary>Left an event's trigger zone (a pickup passed uncollected).</summary>
        RoadZoneExit,
        /// <summary>Entered a dangerous stretch of road.</summary>
        RoadDangerEnter,
        /// <summary>Left the dangerous stretch.</summary>
        RoadDangerExit,
        /// <summary>Drifting off the road's edge.</summary>
        RoadEdgeBump,
        /// <summary>Wheels or armor took a hit.</summary>
        RoadCoachDamage,
        /// <summary>A wheel or armor slot fully broke.</summary>
        RoadCoachBreak,
        /// <summary>A driven-over object hurt the party.</summary>
        RoadPenalty,
        /// <summary>An opt-in interaction stopped the coach and waits.</summary>
        RoadPrompt,
        /// <summary>The Loathing meter advanced.</summary>
        RoadLoathing,

        // Route/node identities (assets/audio/nodes), one tick timbre per destination type -
        // played when a fork menu route gets focus and by minimap review later.
        NodeCombat,
        NodeCache,
        NodeUnknown,
        NodeInn,
        NodeHospital,
        NodeDungeon,
        NodeOasis,
        NodeStore,
        NodeStory,
        NodeWatchtower,
        NodeGuardian,
        NodeDen,
        NodeGate,
        NodeBridge,

        // Combat (assets/audio/combat).
        /// <summary>Focus landed on a valid target for the chosen skill (660 Hz).</summary>
        CombatTargetValid,
        /// <summary>Focus landed on an invalid target for the chosen skill (440 Hz).</summary>
        CombatTargetInvalid,
    }
}
