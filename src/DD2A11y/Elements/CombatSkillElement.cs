using System.Collections.Generic;
using Assets.Code.Skill;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One skill on the combat bar (a regular skill, move, or pass): the skill's name, with the
    /// selected state, remaining limited uses, and usability read live. Enter runs the game's own
    /// skill-pick handler (validity, presentation gating, audio, then target-select). The full
    /// skill card is buffer lines.
    /// </summary>
    public sealed class CombatSkillElement : UIElement {
        private readonly SkillButtonBhv _button;

        public CombatSkillElement(SkillButtonBhv button) {
            _button = button;
        }

        public override bool CanFocus => _button != null && _button.gameObject.activeInHierarchy;

        public override string Label {
            get {
                var skill = Actors.Skill(_button.SkillId);
                return skill != null ? SkillDescription.GetNameText(skill) : UiText.FirstLabel(_button.gameObject);
            }
        }

        public override string Role => S.RoleButton;

        public override string Value {
            get {
                var parts = new List<string>();
                var actor = Actors.Get(_button.ActorGuid);
                if (actor != null && actor.SelectedSkillId == _button.SkillId) {
                    parts.Add(S.StatusSelected);
                }
                var skill = Actors.Skill(_button.SkillId);
                if (skill != null && skill.m_Limit > 0 && actor != null) {
                    string format = GameLoc.TryGet("effect_tooltip_skill_limit");
                    int uses = actor.GetRemainingSkillLimitUses(skill);
                    parts.Add(format == null ? uses.ToString() : string.Format(format, uses));
                }
                if (!_button.IsValid) {
                    parts.Add(S.StatusUnavailable);
                }
                return parts.Count == 0 ? null : SpokenLine.Join(parts.ToArray());
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            // The game's own pick path: validity, the is-presenting gate, selection audio, and
            // the flip into target-select.
            yield return new ElementAction(ActionIds.Activate, () => _button.OnClick());
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            foreach (var line in SkillCard.Lines(_button.SkillId, _button.ActorGuid)) {
                yield return line;
            }
        }
    }
}
