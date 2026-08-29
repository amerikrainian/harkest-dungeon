using Assets.Code.Actor;
using Assets.Code.Library;
using Assets.Code.Skill;
using Assets.Code.Utils;

namespace DD2A11y.Game {
    /// <summary>
    /// A hero's skills-per-rank coverage, computed from the model the same way the game's
    /// aggregate rank pips are (SkillAveragePositionPipGroup): for each of the four hero
    /// ranks, how many of the hero's equipped combat skills can act from it. The crossroads
    /// hero panel and the hero sheet draw this as the "Rank" pip row; a path id applies that
    /// path's skill replacements first, mirroring the path comparison panel's what-if pips.
    /// </summary>
    public static class RankCoverage {
        /// <summary>The equipped-skill count usable from each rank, index 0 = rank 1. Null
        /// when the actor cannot equip skills (no class data).</summary>
        public static int[] LaunchCounts(ActorInstance actor, string previewPathId = null) {
            if (actor?.ActorDataClass == null) {
                return null;
            }
            var skills = SingletonMonoBehaviour<Library<string, ActorDataSkill>>.Instance;
            var counts = new int[4];
            foreach (var equippedId in actor.GetEquippedCombatSkillIds()) {
                var skill = skills.GetLibraryElement(Replaced(equippedId, previewPathId));
                if (skill == null) {
                    continue;
                }
                var launchRanks = skill.LaunchRanks;
                for (int rank = 0; rank < counts.Length; rank++) {
                    if (launchRanks.Contains(rank)) {
                        counts[rank]++;
                    }
                }
            }
            return counts;
        }

        /// <summary>The equipped-skill count able to hit each enemy rank, index 0 = enemy rank 1
        /// (the panel's "Target" pip row). Ally-targeting skills stay out, as in the game's own
        /// row - their reach shows there as a highlight, not a fill. Null without class data.</summary>
        public static int[] TargetCounts(ActorInstance actor, string previewPathId = null) {
            if (actor?.ActorDataClass == null) {
                return null;
            }
            var skills = SingletonMonoBehaviour<Library<string, ActorDataSkill>>.Instance;
            var counts = new int[4];
            foreach (var equippedId in actor.GetEquippedCombatSkillIds()) {
                var skill = skills.GetLibraryElement(Replaced(equippedId, previewPathId));
                if (skill == null || skill.m_IsFriendly) {
                    continue;
                }
                var targetRanks = skill.GetTargetRanks(null);
                for (int rank = 0; rank < counts.Length; rank++) {
                    if (targetRanks.Contains(rank)) {
                        counts[rank]++;
                    }
                }
            }
            return counts;
        }

        /// <summary>Which hero ranks the hero's ally-targeting skills can reach, index 0 =
        /// rank 1 (the Rank pip row's glow). Self-only support stays out, as in the game's
        /// own highlight. All-false when the hero has no such skills; null without class
        /// data.</summary>
        public static bool[] AllyReachRanks(ActorInstance actor, string previewPathId = null) {
            if (actor?.ActorDataClass == null) {
                return null;
            }
            var skills = SingletonMonoBehaviour<Library<string, ActorDataSkill>>.Instance;
            var reach = new bool[4];
            foreach (var equippedId in actor.GetEquippedCombatSkillIds()) {
                var skill = skills.GetLibraryElement(Replaced(equippedId, previewPathId));
                if (skill == null || !skill.m_IsFriendly || skill.m_IsOnlySelfTargetValid) {
                    continue;
                }
                var targetRanks = skill.GetTargetRanks(null);
                for (int rank = 0; rank < reach.Length; rank++) {
                    if (targetRanks.Contains(rank)) {
                        reach[rank] = true;
                    }
                }
            }
            return reach;
        }

        /// <summary>How many combat skills the hero can have equipped - the ladder's full-count
        /// ceiling, the same normalizer the game's pip fills use.</summary>
        public static int EquipLimit(ActorInstance actor)
            => actor.ActorDataClass.m_EquippedCombatSkillLimit;

        // The skill this path swaps in for an equipped one (the game's path-comparison
        // replacement table); the id itself when the path replaces nothing here.
        private static string Replaced(string skillId, string pathId) {
            if (pathId == null) {
                return skillId;
            }
            var replacement = SingletonMonoBehaviour<Library<string, ActorDataSkillReplacement>>
                .Instance.GetLibraryElement(pathId);
            if (replacement == null) {
                return skillId;
            }
            foreach (var source in replacement.SourceSkillReplacements) {
                if (source.Definition.m_FromActorDataSkillId == skillId
                    && source.Definition.m_IsPathComparisonValid) {
                    return source.Definition.m_ToActorDataSkillId;
                }
            }
            return skillId;
        }
    }
}
