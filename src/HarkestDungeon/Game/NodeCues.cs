using Assets.Code.Map.Generation;
using DD2A11y.Core.Audio;

namespace DD2A11y.Game {
    /// <summary>The identity cue for a map node type - one timbre per destination kind, shared
    /// by the fork menu's route focus and the road's node-approach pings.</summary>
    public static class NodeCues {
        public static AudioCue For(NodeType type) {
            if (type == null) {
                return AudioCue.NodeUnknown;
            }
            if (type == NodeType.CACHE || type == NodeType.CACHE_GANG) return AudioCue.NodeCache;
            if (type == NodeType.HOSPITAL) return AudioCue.NodeHospital;
            if (type == NodeType.STORE) return AudioCue.NodeStore;
            if (type == NodeType.WATCH_TOWER) return AudioCue.NodeWatchtower;
            if (type == NodeType.OASIS) return AudioCue.NodeOasis;
            if (type == NodeType.DUNGEON) return AudioCue.NodeDungeon;
            if (type == NodeType.GUARDIAN) return AudioCue.NodeGuardian;
            if (type == NodeType.CREATURE_DEN) return AudioCue.NodeDen;
            if (type == NodeType.GATE) return AudioCue.NodeGate;
            if (type == NodeType.BRIDGE || type == NodeType.BRIDGE_GANG) return AudioCue.NodeBridge;
            if (type == NodeType.INN || type == NodeType.KINGDOM_INN) return AudioCue.NodeInn;
            if (type == NodeType.STORY_CULTIST || type == NodeType.STORY_ASSIST || type == NodeType.STORY_RESIST
                || type == NodeType.STORY_COSMIC || type == NodeType.STORY_HERO || type == NodeType.STORY_HERO_REPLACEMENT
                || type == NodeType.STORY_ASSIST_GANG) {
                return AudioCue.NodeStory;
            }
            return AudioCue.NodeUnknown;
        }
    }
}
