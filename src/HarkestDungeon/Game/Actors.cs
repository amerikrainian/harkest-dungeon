using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Queries;
using Assets.Code.Library;
using Assets.Code.Skill;
using Assets.Code.Token;
using Assets.Code.Utils;
using DD2A11y.Core.Text;

namespace DD2A11y.Game {
    /// <summary>Centralized reads of the game's actor and skill libraries, so every caller
    /// resolves the same live model the same way.</summary>
    public static class Actors {
        /// <summary>The combatant's spoken name: a hero's instance name, else the loc string of
        /// the data id (a monster's name - the same source the game's turn-order tooltips
        /// use).</summary>
        public static string Name(ActorInstance actor) {
            if (actor == null) {
                return null;
            }
            return string.IsNullOrEmpty(actor.ActorName) ? GameLoc.TryGet(actor.ActorDataId) : actor.ActorName;
        }

        /// <summary>The per-battle duplicate-name numbering (the game points at the specific
        /// one only visually, by highlighting the model under the hovered portrait); the
        /// combat screen resets it when the battle ends.</summary>
        public static readonly CombatantNames Numbering = new CombatantNames();

        /// <summary>The combatant's battle identity: its name, with a stable ordinal appended
        /// while several living teammates share it (a pack of Lost Souls). Ordinals follow
        /// first-sight position order, so shuffles never rename anyone; the team is read live,
        /// so a death compacts the surviving numbers down.</summary>
        public static string SpokenName(ActorInstance actor) {
            string name = Name(actor);
            if (name == null) {
                return null;
            }
            var team = Team(friendly: actor.TeamIndex == 0);
            var order = new List<uint>(team.Count);
            var named = new List<KeyValuePair<uint, string>>(team.Count);
            foreach (var teammate in team) {
                order.Add(teammate.m_ActorGuid);
                named.Add(new KeyValuePair<uint, string>(teammate.m_ActorGuid, Name(teammate)));
            }
            Numbering.Observe(order);
            return Numbering.Spoken(actor.m_ActorGuid, name, named);
        }

        /// <summary>One side of the battle in rank order, as the game keeps the team: living
        /// combatants plus the battle-complete classes that still hold a rank and take hits
        /// (corpses, prop monsters), plus kingdoms militia allies fighting in the party's
        /// line. The game's character sheet excludes militia and corpses from its pager -
        /// they have no hero sheet - but on the battlefield they are ordinary combatants.</summary>
        public static List<ActorInstance> Team(bool friendly) {
            var actors = new List<ActorInstance>();
            foreach (uint guid in QueryTeamActors.Trigger(0, friendly).m_TeamActorGuids) {
                var actor = Get(guid);
                if (actor == null) {
                    continue;
                }
                actors.Add(actor);
            }
            actors.Sort((a, b) => a.TeamPosition.CompareTo(b.TeamPosition));
            return actors;
        }

        /// <summary>The actor's visible tokens - the same IsHidden filter the game's own token
        /// icons apply. Hidden tokens are internal logic-control state (their loc text is a
        /// "please file a bug" placeholder) and must never be spoken.</summary>
        public static List<TokenInstance> VisibleTokens(ActorInstance actor) {
            var visible = new List<TokenInstance>();
            var tokens = actor.TokenContainer?.GetInstances();
            if (tokens == null) {
                return visible;
            }
            foreach (var token in tokens) {
                if (!token.IsHidden) {
                    visible.Add(token);
                }
            }
            return visible;
        }

        /// <summary>HP and stress the way the game's status bars caption them
        /// ("HP 30/30, Stress 2/10"); monsters have no stress bar and read HP only.</summary>
        public static string StatusLine(ActorInstance actor) {
            if (actor == null) {
                return null;
            }
            string hpFormat = GameLoc.TryGet("status_bar_health");
            string hp = hpFormat == null ? (int)actor.DisplayedHp + "/" + (int)actor.DisplayedHpMax
                : string.Format(hpFormat, (int)actor.DisplayedHp, (int)actor.DisplayedHpMax);
            string stressFormat = GameLoc.TryGet("status_bar_stress");
            string stress = stressFormat == null ? null
                : string.Format(stressFormat, (int)actor.Stress, (int)actor.StressMax);
            return SpokenLine.Join(hp, stress);
        }

        public static ActorInstance Get(uint actorGuid) {
            if (!SingletonMonoBehaviour<Library<uint, ActorInstance>>.HasInstance()) {
                return null;
            }
            return SingletonMonoBehaviour<Library<uint, ActorInstance>>.Instance.GetLibraryElement(actorGuid);
        }

        public static ActorDataSkill Skill(string skillId) {
            if (!SingletonMonoBehaviour<Library<string, ActorDataSkill>>.HasInstance()) {
                return null;
            }
            return SingletonMonoBehaviour<Library<string, ActorDataSkill>>.Instance.GetLibraryElement(skillId);
        }
    }
}
