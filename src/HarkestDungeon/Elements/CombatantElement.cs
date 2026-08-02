using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Events;
using Assets.Code.Buff;
using Assets.Code.Combat;
using Assets.Code.UI;
using Assets.Code.UI.Events;
using Assets.Code.UI.Tooltips;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One combatant on the battlefield: name, with rank and HP. While a skill is waiting for
    /// its target, validity rides as audio (the screen's beeps): an invalid target's line leads
    /// with the reason it cannot be hit, a valid one ends with the game's own hit/crit/heal
    /// preview. Enter sends the game's own actor-pick event (the mouse click equivalent:
    /// executes the selected skill on a valid target, otherwise the game ignores it); the
    /// inspect action opens the hero sheet. The buffer is the full status readout: HP, stress,
    /// then one line per token, dot, and combat buff, all from the game's own describers.
    /// </summary>
    public sealed class CombatantElement : UIElement {
        private readonly uint _guid;
        private readonly bool _friendly;
        private readonly SkillSelectionBhv _skillSelection;

        public CombatantElement(uint guid, bool friendly, SkillSelectionBhv skillSelection) {
            _guid = guid;
            _friendly = friendly;
            _skillSelection = skillSelection;
        }

        public uint Guid => _guid;

        private ActorInstance Actor => Actors.Get(_guid);

        public override bool CanFocus => Actor != null;

        public override string Label {
            get {
                string name = Actors.Name(Actor);
                string reason = PickPending(out var performer, out var skill)
                                && !Targeting.IsValidTarget(performer, _guid)
                    ? Targeting.InvalidReason(performer, skill, Actor) : null;
                return reason == null ? name : SpokenLine.Join(reason, name);
            }
        }

        public override string Value {
            get {
                var actor = Actor;
                if (actor == null) {
                    return null;
                }
                string preview = PickPending(out var performer, out _) && Targeting.IsValidTarget(performer, _guid)
                    ? Targeting.PreviewText(performer, _guid) : null;
                return SpokenLine.Join(RankText(actor), HpText(actor), preview);
            }
        }

        private bool PickPending(out ActorInstance performer, out Assets.Code.Skill.ActorDataSkill skill) {
            performer = null;
            skill = null;
            return _skillSelection != null
                && _skillSelection.CurrentInputState == SkillSelectionBhv.InputState.ACTOR_SELECT
                && Targeting.TryGetPick(out performer, out skill);
        }

        // The game's rank, not the team-list index - a size-2 monster spans two ranks, so the
        // combatant behind it stands at rank 3, not slot 2.
        private static string RankText(ActorInstance actor) {
            string format = GameLoc.TryGet("effect_tooltip_position");
            int rank = actor.GetFrontRank() + 1;
            return format == null ? rank.ToString() : string.Format(format, rank);
        }

        private static string HpText(ActorInstance actor) {
            string format = GameLoc.TryGet("status_bar_health");
            int hp = (int)actor.DisplayedHp;
            int max = (int)actor.DisplayedHpMax;
            return format == null ? hp + "/" + max : string.Format(format, hp, max);
        }

        public override IEnumerable<ElementAction> GetActions() {
            // The same event a mouse click on the actor sends; in target-select the battle state
            // machine validates and executes, otherwise it is the game's browse/no-op.
            yield return new ElementAction(ActionIds.Activate,
                () => EventSelectActor.Trigger(_guid, isUserInput: true));
            yield return new ElementAction("inspect", () => EventInspectActor.Trigger(_guid));
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            var actor = Actor;
            if (actor == null) {
                yield break;
            }
            if (_friendly) {
                string stressFormat = GameLoc.TryGet("status_bar_stress");
                if (stressFormat != null) {
                    yield return string.Format(stressFormat, (int)actor.Stress, (int)actor.StressMax);
                }
            }
            var tokens = Actors.VisibleTokens(actor);
            if (tokens.Count > 0) {
                foreach (var line in TokenTooltipBhv.MakeTooltip(tokens).Split('\n')) {
                    if (!string.IsNullOrWhiteSpace(line)) {
                        yield return line;
                    }
                }
            }
            var dots = actor.DotContainer?.GetInstances();
            if (dots != null && dots.Count > 0) {
                foreach (var line in DotTooltipBhv.MakeTooltipText(dots, condense: false).Split('\n')) {
                    if (!string.IsNullOrWhiteSpace(line)) {
                        yield return line;
                    }
                }
            }
            // Stat buffs and debuffs, filtered to the ones the game's own actor panel shows.
            var buffs = actor.BuffContainer?.GetInstances();
            if (buffs != null) {
                foreach (var buff in buffs) {
                    if (buff?.Definition == null || !buff.Definition.IsEligibleToShowAsCombatUi) {
                        continue;
                    }
                    string text = BuffDescription.GetDescriptionWithDuration(buff);
                    if (string.IsNullOrWhiteSpace(text)) {
                        continue;
                    }
                    foreach (var line in text.Split('\n')) {
                        if (!string.IsNullOrWhiteSpace(line)) {
                            yield return line;
                        }
                    }
                }
            }
        }
    }
}
