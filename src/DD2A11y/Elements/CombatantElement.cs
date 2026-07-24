using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Events;
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
    /// One combatant on the battlefield: name, with rank, HP, and - while a skill is waiting for
    /// its target - whether this combatant is a valid target for it. Enter sends the game's own
    /// actor-pick event (the mouse click equivalent: executes the selected skill on a valid
    /// target, otherwise the game ignores it); the inspect action opens the hero sheet. The
    /// buffer is the full status readout: HP, stress, then one line per token and per dot, all
    /// from the game's own describers.
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

        private ActorInstance Actor => Actors.Get(_guid);

        public override bool CanFocus => Actor != null;

        public override string Label => Actors.Name(Actor);

        public override string Value {
            get {
                var actor = Actor;
                if (actor == null) {
                    return null;
                }
                string rank = RankText(actor);
                string hp = HpText(actor);
                return SpokenLine.Join(TargetingText(), rank, hp);
            }
        }

        // While the acting hero's chosen skill waits for a target, every combatant carries its
        // validity for that skill - the same check the game runs on a click.
        private string TargetingText() {
            if (_skillSelection == null
                || _skillSelection.CurrentInputState != SkillSelectionBhv.InputState.ACTOR_SELECT) {
                return null;
            }
            var current = CurrentActor();
            if (current?.Controller == null || current.SelectedSkillId == null) {
                return null;
            }
            return current.Controller.GetIsValidSkillTarget(current.SelectedSkillId, _guid)
                ? S.CombatTargetValid : S.CombatTargetInvalid;
        }

        private static ActorInstance CurrentActor() {
            if (!SingletonMonoBehaviour<CombatBhv>.HasInstance()) {
                return null;
            }
            var combat = SingletonMonoBehaviour<CombatBhv>.Instance;
            if (combat.CurrentBattleState == BattleState.INACTIVE) {
                return null;
            }
            return Actors.Get(combat.CurrentActorGuid);
        }

        private static string RankText(ActorInstance actor) {
            string format = GameLoc.TryGet("effect_tooltip_position");
            int rank = actor.TeamPosition + 1;
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
            var tokens = actor.TokenContainer?.GetInstances();
            if (tokens != null && tokens.Count > 0) {
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
        }
    }
}
