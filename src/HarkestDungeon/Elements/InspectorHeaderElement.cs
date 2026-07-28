using System;
using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Events;
using Assets.Code.Game;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// The inspector's identity line: the inspected combatant's name (with the boss blessing
    /// mark), HP, stress on heroes, and speed. The buffer carries death's door and the blessing
    /// description. Enter (or C) on a party hero opens their character sheet - the same Submit
    /// the game's own view answers.
    /// </summary>
    public sealed class InspectorHeaderElement : UIElement {
        private readonly Func<ActorInstance> _actor;

        public InspectorHeaderElement(Func<ActorInstance> actor) {
            _actor = actor;
        }

        public override bool CanFocus => _actor() != null;

        public override string Label {
            get {
                var actor = _actor();
                if (actor == null) {
                    return null;
                }
                return SpokenLine.Join(Actors.Name(actor), actor.IsOrdained ? S.StatusBlessed : null);
            }
        }

        public override string Value {
            get {
                var actor = _actor();
                if (actor == null) {
                    return null;
                }
                var parts = new List<string> { Stat("status_bar_health", (int)actor.DisplayedHp, (int)actor.DisplayedHpMax) };
                if (actor.TeamIndex == 0) {
                    parts.Add(Stat("status_bar_stress", (int)actor.Stress, (int)actor.StressMax));
                }
                parts.Add(S.SheetSpeed((int)actor.GetClampedStatValue(ActorStatType.SPEED)));
                return SpokenLine.Join(parts.ToArray());
            }
        }

        private static string Stat(string locKey, int current, int max) {
            string format = GameLoc.TryGet(locKey);
            return format == null ? current + "/" + max : string.Format(format, current, max);
        }

        public override IEnumerable<ElementAction> GetActions() {
            var actor = _actor();
            if (actor == null || actor.TeamIndex != 0
                || !Singleton<GameTypeMgr>.Instance.RosterManager.GetIsActorInParty(actor.m_ActorGuid)) {
                yield break;
            }
            uint guid = actor.m_ActorGuid;
            yield return new ElementAction(ActionIds.Activate, () => EventInspectActor.Trigger(guid));
            yield return new ElementAction("inspect", () => EventInspectActor.Trigger(guid));
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            var actor = _actor();
            if (actor == null) {
                yield break;
            }
            if (actor.GetIsStatusActive(ActorStatusType.DEATHS_DOOR)) {
                yield return S.CombatDeathsDoor(Actors.Name(actor));
            }
            foreach (var line in Study.BlessingLines(actor)) {
                yield return line;
            }
        }
    }
}
