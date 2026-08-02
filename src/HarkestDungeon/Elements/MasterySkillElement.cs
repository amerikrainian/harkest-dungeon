using System.Collections.Generic;
using System.Linq;
using Assets.Code.Skill;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One skill on the inn's Mastery Trainer: the skill's own name, its state ("mastered",
    /// "selected" while queued for the Apply press, "unavailable" when it cannot be picked or
    /// the points are short), and the full skill card as buffer lines. Enter queues the skill
    /// through the trainer's own selection (the mouse gesture is a hold); Apply and Reset are
    /// the screen's own buttons.
    /// </summary>
    public sealed class MasterySkillElement : SelectableElement {
        private readonly UpgradeSkillButton _button;
        private readonly InnUpgradeSkillsBhv _panel;

        public MasterySkillElement(UpgradeSkillButton button, InnUpgradeSkillsBhv panel, Selectable selectable)
            : base(selectable) {
            _button = button;
            _panel = panel;
        }

        public override string Label {
            get {
                var skill = Actors.Skill(_button.SkillId);
                return skill != null ? SkillDescription.GetNameText(skill) : base.Label;
            }
        }

        private bool IsSelected {
            get {
                var actor = _button.ActorInstance;
                string id = _button.SkillId;
                if (actor == null || string.IsNullOrEmpty(id)) {
                    return false;
                }
                return _panel.IsSkillSelectedForUpgrade(actor, id) || _panel.IsSkillSelectedForUnlock(actor, id)
                    || _panel.IsSkillSelectedForUpgrade(actor, id + "_u");
            }
        }

        public override string Status => IsSelected ? S.StatusSelected : null;

        public override string Value {
            get {
                var actor = _button.ActorInstance;
                string id = _button.SkillId;
                if (actor == null || string.IsNullOrEmpty(id)) {
                    return null;
                }
                if (actor.GetUpgradedCombatSkillIds().Contains(id)) {
                    return S.SkillMastered;
                }
                // A queued skill is no longer upgradable, which is the queue at work, not a lock.
                if (IsSelected) {
                    return null;
                }
                bool pickable = (_panel.GetIsUpgradable(actor, id) || _panel.GetIsUnlockable(actor, id))
                    && _panel.CanAffordSkill;
                return pickable ? null : S.StatusUnavailable;
            }
        }

        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                var actor = _button.ActorInstance;
                string id = _button.SkillId;
                if (actor == null || actor.GetUpgradedCombatSkillIds().Contains(id)
                    || !(_panel.GetIsUpgradable(actor, id) || _panel.GetIsUnlockable(actor, id))
                    || !_panel.CanAffordSkill) {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                    return;
                }
                _panel.TrySelectSkillToUnlock(actor, id);
            });
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            foreach (var line in SkillCard.Lines(_button.SkillId, _button.ActorGuid)) {
                yield return line;
            }
        }
    }
}
