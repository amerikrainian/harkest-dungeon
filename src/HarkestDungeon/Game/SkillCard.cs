using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Skill;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// The full spoken card for one combat skill, composed from the game's own SkillDescription
    /// strings - the same source the visual tooltip renders: usable ranks and targets,
    /// damage/crit/cooldown/limit, the per-target effects, and the melee/ranged tag. One buffer
    /// line per fact. <see cref="UpgradeBufferLines"/> is the mastery preview the sighted
    /// tooltip carries beside the card, reviewed as its own buffer.
    /// </summary>
    public static class SkillCard {
        // Every skill's mastered variant lives in the library under its id plus this suffix,
        // and a hero's buttons carry the suffixed id once the skill is mastered.
        private const string UpgradeSuffix = "_u";

        /// <summary>Whether this id is a skill's mastered variant.</summary>
        public static bool IsMasteredId(string skillId)
            => skillId != null && skillId.EndsWith(UpgradeSuffix, System.StringComparison.Ordinal);

        /// <summary>The mastered variant's id when the library has one, else the id itself.</summary>
        public static string MasteredId(string skillId)
            => Actors.Skill(skillId + UpgradeSuffix) != null ? skillId + UpgradeSuffix : skillId;

        /// <summary>The upgrade buffer's spoken name: the game's own header over the mastery
        /// preview, its trailing colon trimmed (list punctuation, not information).</summary>
        public static string UpgradeTitle() {
            string title = GameLoc.TryGet("upgrade_skill_tooltip_upgrade_title");
            return title == null ? S.BufferMastery : title.TrimEnd(':', ' ');
        }

        /// <summary>The upgrade buffer for one skill: the mastery preview when the skill still
        /// has one, else the authored no-upgrade line (the skill is already mastered, or has no
        /// mastered variant). Never empty, so the buffer answers on every skill.</summary>
        public static IEnumerable<string> UpgradeBufferLines(string skillId, uint actorGuid) {
            bool any = false;
            foreach (var line in UpgradeLines(skillId, actorGuid)) {
                any = true;
                yield return line;
            }
            if (!any) {
                yield return S.SkillNoUpgrade;
            }
        }

        // The mastery preview the game shows beside an unmastered skill's card (the trainer
        // tooltip's second half, the sheet tooltip's hold-to-expand): the mastered variant's
        // stat bar and per-target effects. Empty when no mastered variant exists (move, pass,
        // already-suffixed ids).
        private static IEnumerable<string> UpgradeLines(string skillId, uint actorGuid) {
            if (IsMasteredId(skillId)) {
                yield break;
            }
            var upgraded = Actors.Skill(skillId + UpgradeSuffix);
            if (upgraded == null) {
                yield break;
            }
            string topBar = SkillDescription.GetUpgradeTopBarString(upgraded, new[] { skillId });
            if (!string.IsNullOrWhiteSpace(topBar)) {
                foreach (var line in topBar.Split('\n')) {
                    if (!string.IsNullOrWhiteSpace(line)) {
                        yield return line;
                    }
                }
            }
            foreach (var result in SkillDescription.GetResultStringsByTargetType(upgraded, showIgnores: false, actorGuid)) {
                if (!string.IsNullOrWhiteSpace(result)) {
                    yield return result;
                }
            }
        }

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
