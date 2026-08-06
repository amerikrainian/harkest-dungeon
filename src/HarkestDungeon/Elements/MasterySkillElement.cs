using System.Collections.Generic;
using System.Linq;
using Assets.Code.Skill;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One skill on the inn's Mastery Trainer: the skill's own name, its state ("mastered",
    /// "selected" while queued for the Apply press, "unavailable" when it cannot be picked or
    /// the points are short), and the full skill card as buffer lines - the mastered card once
    /// mastered, else the current card. The mastery preview (the sighted tooltip's second half)
    /// is the upgrade buffer, and the trainer's hero fills the hero buffer. Enter queues the
    /// skill through the trainer's own selection (the mouse gesture is a hold); Apply and Reset
    /// are the screen's own buttons.
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

        public override string Status {
            get {
                var actor = _button.ActorInstance;
                string id = _button.SkillId;
                if (actor == null || string.IsNullOrEmpty(id)) {
                    return null;
                }
                if (actor.GetUpgradedCombatSkillIds().Contains(id)) {
                    return S.SkillMastered;
                }
                if (_panel.IsSkillSelectedForUpgrade(actor, id) || _panel.IsSkillSelectedForUnlock(actor, id)
                    || _panel.IsSkillSelectedForUpgrade(actor, id + "_u")) {
                    return S.StatusSelected;
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

        protected override IEnumerable<string> GetDetailLines() {
            foreach (var line in SkillCard.Lines(DisplayedSkillId(), _button.ActorGuid)) {
                yield return line;
            }
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey) {
            if (bufferKey == BufferKeys.Upgrade) {
                // The mastered variant's id once mastered, so the preview folds into the
                // no-upgrade line instead of re-offering the upgrade the hero already owns.
                return SkillCard.UpgradeBufferLines(DisplayedSkillId(), _button.ActorGuid);
            }
            if (bufferKey == BufferKeys.Hero) {
                return HeroStatus.Lines(_button.ActorGuid);
            }
            return base.GetSideBufferLines(bufferKey);
        }

        // The id whose card the trainer is showing: the mastered variant once mastered, else
        // the button's own.
        private string DisplayedSkillId() {
            string id = _button.SkillId;
            var actor = _button.ActorInstance;
            bool mastered = actor != null && actor.GetUpgradedCombatSkillIds().Contains(id);
            return mastered ? SkillCard.MasteredId(id) : id;
        }
    }
}
