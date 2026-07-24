using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Skill;
using Assets.Code.UI;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Elements {
    /// <summary>
    /// One combat skill on the hero sheet: the skill's name with its equipped state, toggled by
    /// Enter through the game's own skill button (equip rules, audio, notifications). The full
    /// skill card is buffer lines: usable ranks and targets, damage/crit/cooldown, the per-target
    /// effects, and the melee/ranged tag - all composed from the game's own SkillDescription
    /// strings, the same source the visual tooltip renders.
    /// </summary>
    public sealed class SkillEquipElement : UIElement {
        private static readonly AccessTools.FieldRef<CharacterSheetStatsUiBhv, List<GameObject>> SkillButtonsField =
            AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, List<GameObject>>("m_combatSkillsAdded");

        private readonly CharacterSheetUiBhv _sheet;
        private readonly CharacterSheetStatsUiBhv _stats;
        private readonly string _skillId;

        public SkillEquipElement(CharacterSheetUiBhv sheet, CharacterSheetStatsUiBhv stats, string skillId) {
            _sheet = sheet;
            _stats = stats;
            _skillId = skillId;
        }

        public override string Label {
            get {
                var skill = Actors.Skill(_skillId);
                return skill == null ? _skillId : SkillDescription.GetNameText(skill);
            }
        }

        public override string Role => S.RoleToggle;

        public override string Value {
            get {
                var actor = Actors.Get(_sheet.ActorGuid);
                if (actor == null) {
                    return null;
                }
                string state = actor.GetCombatSkillEquipped(_skillId) ? S.StatusOn : S.StatusOff;
                return SpokenLine.Join(state, _sheet.IsSkillsEditable ? null : S.StatusUnavailable);
            }
        }

        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, Toggle);
        }

        // The game's own click handler owns the rules (editable, mode, minimum loadout) and the
        // audio feedback for a refused toggle.
        private void Toggle() {
            var button = FindButton();
            if (button == null) {
                Plugin.Log.LogWarning("SkillEquipElement: no live button for skill " + _skillId);
                return;
            }
            button.OnClick();
        }

        private CharacterSheetSkillButtonBhv FindButton() {
            foreach (var holder in SkillButtonsField(_stats)) {
                var button = holder == null ? null : holder.GetComponent<CharacterSheetSkillButtonBhv>();
                if (button != null && button.SkillId == _skillId) {
                    return button;
                }
            }
            return null;
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            var skill = Actors.Skill(_skillId);
            if (skill == null) {
                yield break;
            }

            if (SkillDescription.TryGetRankInfo(skill, 4, out _, out var launchRanks, out var targetRanks, out var multiHits)) {
                string launch = RankList(launchRanks, null);
                if (launch.Length > 0) {
                    yield return Format("effect_tooltip_position", launch);
                }
                string target = RankList(targetRanks, multiHits);
                if (target.Length > 0) {
                    yield return Format("effect_tooltip_target", target);
                }
            }

            var actor = Actors.Get(_sheet.ActorGuid);
            string topBar = SkillDescription.GetTopBarString(skill, actor);
            if (!string.IsNullOrWhiteSpace(topBar)) {
                foreach (var line in topBar.Split('\n')) {
                    if (!string.IsNullOrWhiteSpace(line)) {
                        yield return line;
                    }
                }
            }

            foreach (var result in SkillDescription.GetResultStringsByTargetType(skill, showIgnores: false, _sheet.ActorGuid)) {
                if (!string.IsNullOrWhiteSpace(result)) {
                    yield return result;
                }
            }

            if (skill.m_Tags.Contains("melee")) {
                yield return GameLoc.TryGet("skill_tag_melee");
            } else if (skill.m_Tags.Contains("ranged")) {
                yield return GameLoc.TryGet("skill_tag_ranged");
            }
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
