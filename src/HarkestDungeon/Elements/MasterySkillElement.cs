using System.Collections.Generic;
using System.Linq;
using Assets.Code.Data;
using Assets.Code.Skill;
using Assets.Code.Skill.Events;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Nav;
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
    /// skill through the trainer's own selection (the mouse gesture is a hold) behind the
    /// game's own press gate, which at a Kingdoms inn holds the trainer's unlock and mastery
    /// limits; Apply and Reset are the screen's own buttons.
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
                var skill = Actors.Skill(DisplayedSkillId());
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
                if (IsMastered()) {
                    return S.SkillMastered;
                }
                if (_panel.IsSkillSelectedForUpgrade(actor, id) || _panel.IsSkillSelectedForUnlock(actor, id)
                    || _panel.IsSkillSelectedForUpgrade(actor, id + "_u")) {
                    return S.StatusSelected;
                }
                return Pickable ? null : S.StatusUnavailable;
            }
        }

        // The game's own press gate (UpgradeSkillButton.ApplySkillButtonDown): the row's limit
        // flags - at a Kingdoms inn, a trainer whose upgrades have not opened mastering, or an
        // unlock limit the hero has reached - then the step this press takes, unlocking a
        // skill the hero lacks or else mastering, against the trainer's matching limit, then
        // the points. The trainer's limit methods alone read a limit of zero as no limit; the
        // row's flags, refreshed by the game after every pick, read it as locked.
        private bool Pickable {
            get {
                var actor = _button.ActorInstance;
                string id = _button.SkillId;
                var context = _button.GetComponent<DataContextBhv>();
                if (context.GetBoolValue("kingdom_upgrade_locked_unpurchaseable") || context.GetBoolValue("mastery_locked")) {
                    return false;
                }
                var skill = actor.GetCombatSkillInstance(id);
                bool unlocking = skill != null && !skill.GetIsUnlocked() && !_panel.IsSkillSelectedForUnlock(actor, id);
                bool allowed = unlocking ? _panel.GetIsUnlockable(actor, id) : _panel.GetIsUpgradable(actor, id);
                return allowed && _panel.CanAffordSkill;
            }
        }

        public override bool ReannounceOnActivate => true;

        public override string GlossaryContext => DisplayedSkillId();

        public override IEnumerable<ElementAction> GetActions() {
            yield return ElementAction.Refusable(ActionIds.Activate, () => {
                var actor = _button.ActorInstance;
                string id = _button.SkillId;
                if (actor == null || IsMastered() || !Pickable) {
                    return false;
                }
                _panel.TrySelectSkillToUnlock(actor, id);
                // The game's completed hold fires this after its pick; the skills widget
                // answers by refreshing every row's limit flags against the new queue.
                EventSkillUpgradeExecuted.Trigger(id, actor.ActorGuid);
                return true;
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

        // Mastery as the game's row presents it (UpgradeSkillButton.IsSkillVisiblyUpgraded):
        // the hero's committed masteries, plus a skill selected for upgrade this visit - its
        // points are already spent and the row wears the full mastered look; only the
        // trainer's Reset button distinguishes the two. The committed check runs on the
        // normalized id: trainer buttons carry the unlock id - the mastered variant's -
        // whatever the mastery state (the game's own row strips the suffix before display
        // the same way).
        private bool IsMastered() {
            var actor = _button.ActorInstance;
            if (actor == null) {
                return false;
            }
            string id = _button.SkillId;
            if (_panel.IsSkillSelectedForUnlock(actor, id)) {
                return _panel.IsSkillSelectedForUpgrade(actor, id + "_u");
            }
            return _panel.IsSkillSelectedForUpgrade(actor, id)
                || actor.GetUpgradedCombatSkillIds()
                    .Contains(SkillCard.MasteredId(SkillCard.BaseId(id)));
        }

        // The id whose card the trainer is showing, matching the game's own tooltip: the
        // mastered variant once mastered, else the base skill - whose mastery preview then
        // fills the upgrade buffer.
        private string DisplayedSkillId() {
            string baseId = SkillCard.BaseId(_button.SkillId);
            return IsMastered() ? SkillCard.MasteredId(baseId) : baseId;
        }
    }
}
