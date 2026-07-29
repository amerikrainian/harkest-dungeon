using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Skill;

namespace DD2A11y.Game {
    /// <summary>
    /// The full spoken card for one combat skill, composed from the game's own SkillDescription
    /// strings - the same source the visual tooltip renders: usable ranks and targets,
    /// damage/crit/cooldown/limit, the per-target effects, and the melee/ranged tag. One buffer
    /// line per fact.
    /// </summary>
    public static class SkillCard {
        public static IEnumerable<string> Lines(string skillId, uint actorGuid) {
            var skill = Actors.Skill(skillId);
            if (skill == null) {
                yield break;
            }

            foreach (var line in RankAndTargetLines(skill)) {
                yield return line;
            }

            var actor = Actors.Get(actorGuid);
            string topBar = SkillDescription.GetTopBarString(skill, actor);
            if (!string.IsNullOrWhiteSpace(topBar)) {
                foreach (var line in topBar.Split('\n')) {
                    if (!string.IsNullOrWhiteSpace(line)) {
                        yield return line;
                    }
                }
            }

            foreach (var result in SkillDescription.GetResultStringsByTargetType(skill, showIgnores: false, actorGuid)) {
                if (!string.IsNullOrWhiteSpace(result)) {
                    yield return result;
                }
            }

            string tag = TypeTag(skill);
            if (tag != null) {
                yield return tag;
            }
        }

        /// <summary>The studied-enemy card, mirroring the game's own academic skill row: ranks
        /// and targets, then only the tokens and dots the skill applies. The full effect
        /// renderer is shown only for player skills - on enemy-only skills its internal effects
        /// (AI class changes) have no player-facing strings and read as raw ids.</summary>
        public static IEnumerable<string> TokenViewLines(string skillId) {
            var skill = Actors.Skill(skillId);
            if (skill == null) {
                yield break;
            }

            foreach (var line in RankAndTargetLines(skill)) {
                yield return line;
            }

            foreach (var result in SkillDescription.GetTokensAndDotsByTargetType(skill)) {
                if (!string.IsNullOrWhiteSpace(result)) {
                    yield return result;
                }
            }

            string tag = TypeTag(skill);
            if (tag != null) {
                yield return tag;
            }
        }

        private static IEnumerable<string> RankAndTargetLines(ActorDataSkill skill) {
            if (!SkillDescription.TryGetRankInfo(skill, 4, out _, out var launchRanks, out var targetRanks, out var multiHits)) {
                yield break;
            }
            string launch = RankList(launchRanks, null);
            if (launch.Length > 0) {
                yield return Format("effect_tooltip_position", launch);
            }
            string target = RankList(targetRanks, multiHits);
            if (target.Length > 0) {
                yield return Format("effect_tooltip_target", target);
            }
        }

        private static string TypeTag(ActorDataSkill skill) {
            if (skill.m_Tags.Contains("melee")) {
                return GameLoc.TryGet("skill_tag_melee");
            }
            if (skill.m_Tags.Contains("ranged")) {
                return GameLoc.TryGet("skill_tag_ranged");
            }
            return null;
        }

        // The used ranks in ascending order ("1 2"); a multi-hit pair joins with "+" ("1+2"),
        // mirroring the game's own textual targeting rendering.
        private static string RankList(bool[] active, bool[] multiHits) {
            var sb = new StringBuilder();
            for (int i = 0; i < active.Length; i++) {
                if (!active[i]) {
                    continue;
                }
                if (sb.Length > 0) {
                    bool joined = multiHits != null && i > 0 && active[i - 1] && multiHits[i - 1];
                    sb.Append(joined ? "+" : " ");
                }
                sb.Append(i + 1);
            }
            return sb.ToString();
        }

        // The game's own "Rank: {0}" / "Target: {0}" framing; falls back to the bare list if the
        // key ever disappears.
        private static string Format(string locKey, string ranks) {
            string format = GameLoc.TryGet(locKey);
            return format == null ? ranks : string.Format(format, ranks);
        }
    }
}
