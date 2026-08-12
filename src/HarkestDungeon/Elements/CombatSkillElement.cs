using System.Collections.Generic;
using Assets.Code.Combat.Queries;
using Assets.Code.Skill;
using Assets.Code.UI;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One skill on the combat bar (a regular skill, a combat item, move, or pass): the skill's
    /// name, with the selected and mastered states, remaining limited uses, an item's quantity,
    /// and usability read live. Enter runs the game's own skill-pick handler (validity,
    /// presentation gating, audio, then target-select). The full skill card is buffer lines,
    /// followed by any affinity change the skill telegraphs (the icon the game shows on the
    /// responding hero).
    /// </summary>
    public sealed class CombatSkillElement : UIElement {
        private readonly SkillButtonBhv _button;
        private readonly SkillSelectionBhv _selection;
        private readonly int _index;

        public CombatSkillElement(SkillButtonBhv button, SkillSelectionBhv selection = null, int index = 0) {
            _button = button;
            _selection = selection;
            _index = index;
        }

        public string SkillId => _button.SkillId;

        public uint ActorGuid => _button.ActorGuid;

        public override bool CanFocus
            => _button != null && _button.gameObject.activeInHierarchy && !IsDuplicate();

        // The game's unlock bookkeeping can grant a hero an always-equipped copy of a skill the
        // player also has equipped, putting the same skill id on two live bar buttons. Its own
        // handlers resolve buttons by skill id and take the first match, so the extra button is
        // indistinguishable from the first in label, card, and effect; only the first reads,
        // and it carries the grant as a buffer line (the hero holds the skill beyond the
        // loadout slot the player filled).
        private bool IsDuplicate() => HasSameSkillButton(0, _index);

        private bool HasGrantedCopy() => HasSameSkillButton(0, _selection == null ? 0 : _selection.SkillButtonCount);

        private bool HasSameSkillButton(int from, int to) {
            if (_selection == null) {
                return false;
            }
            string id = _button.SkillId;
            if (string.IsNullOrEmpty(id)) {
                return false;
            }
            for (int i = from; i < to; i++) {
                if (i == _index) {
                    continue;
                }
                var other = _selection.GetSkillButton(i);
                if (other.gameObject.activeInHierarchy && other.SkillId == id) {
                    return true;
                }
            }
            return false;
        }

        public override string Label {
            get {
                var skill = Actors.Skill(_button.SkillId);
                return skill != null ? SkillDescription.GetNameText(skill) : UiText.FirstLabel(_button.gameObject);
            }
        }

        public override string Role => S.RoleButton;

        public override string GlossaryContext => _button.SkillId;

        public override string Status {
            get {
                var actor = Actors.Get(_button.ActorGuid);
                return SpokenLine.Join(
                    actor != null && actor.SelectedSkillId == _button.SkillId ? S.StatusSelected : null,
                    SkillCard.IsMasteredId(_button.SkillId) ? S.SkillMastered : null);
            }
        }

        public override string Value {
            get {
                var parts = new List<string>();
                var actor = Actors.Get(_button.ActorGuid);
                var skill = Actors.Skill(_button.SkillId);
                if (skill != null && skill.m_Limit > 0 && actor != null) {
                    string format = GameLoc.TryGet("effect_tooltip_skill_limit");
                    int uses = actor.GetRemainingSkillLimitUses(skill);
                    parts.Add(format == null ? uses.ToString() : string.Format(format, uses));
                }
                // A combat item's stack, the bar's own "Quantity: 2" beside the item skill.
                if (actor != null && !string.IsNullOrEmpty(_button.SkillId)) {
                    int count = actor.GetItemCountFromSkillId(_button.SkillId);
                    if (count > 0) {
                        string format = GameLoc.TryGet("combat_item_display_quantity");
                        parts.Add(format == null ? count.ToString() : string.Format(format, count));
                    }
                }
                if (!_button.IsValid) {
                    // The game's own wording for why the skill is grey (wrong rank, on
                    // cooldown, out of uses, no valid target...); the bare fallback covers a
                    // validity type with no authored string.
                    parts.Add(InvalidReasonText() ?? S.StatusUnavailable);
                }
                return parts.Count == 0 ? null : SpokenLine.Join(parts.ToArray());
            }
        }

        public string InvalidReasonText() {
            var query = QueryIsValidSkill.Trigger(_button.ActorGuid, _button.SkillId);
            if (query.m_ValidityType == null || query.IsValid) {
                return null;
            }
            return GameLoc.TryGet("invalid_skill_reason_" + query.m_ValidityType);
        }

        public override IEnumerable<ElementAction> GetActions() {
            // The game's own pick path: validity, the is-presenting gate, selection audio, and
            // the flip into target-select.
            yield return new ElementAction(ActionIds.Activate, () => _button.OnClick());
        }

        protected override IEnumerable<string> GetDetailLines() {
            foreach (var line in SkillCard.Lines(_button.SkillId, _button.ActorGuid)) {
                yield return line;
            }
            var actor = Actors.Get(_button.ActorGuid);
            if (actor != null) {
                foreach (var line in Targeting.AffinityPreviews(actor, _button.SkillId)) {
                    yield return line;
                }
            }
            if (HasGrantedCopy()) {
                yield return S.CombatSkillAlsoGranted;
            }
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => bufferKey == BufferKeys.Hero
                ? HeroStatus.Lines(_button.ActorGuid) : base.GetSideBufferLines(bufferKey);
    }
}
