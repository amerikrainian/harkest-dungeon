using System.Collections.Generic;
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

        public override string Status {
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

        protected override IEnumerable<string> GetDetailLines() {
            foreach (var line in SkillCard.Lines(_skillId, _sheet.ActorGuid)) {
                yield return line;
            }
        }
    }
}
