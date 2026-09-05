using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Queries;
using Assets.Code.Library;
using Assets.Code.Run;
using Assets.Code.Skill;
using Assets.Code.Token;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

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

        /// <summary>The actor's rank the way the game's own position tooltip captions it
        /// ("Rank: 2", 1 = the front line) - the game's rank, not the team-list index, so a
        /// size-2 monster's neighbor reads the rank behind both of its. Null while the actor
        /// holds no position yet. Valid outside combat too: the party keeps its marching-order
        /// positions synced to the roster.</summary>
        public static string RankText(ActorInstance actor) {
            if (actor == null || !actor.GetIsTeamPositionSet()) {
                return null;
            }
            string format = GameLoc.TryGet("effect_tooltip_position");
            int rank = actor.GetFrontRank() + 1;
            return format == null ? rank.ToString() : string.Format(format, rank);
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

        /// <summary>The status word for the hero's run goal: "complete" once the hero has met
        /// it, else null. The game marks completion only visually - the row struck through
        /// with a checkmark - and the goal text's own progress count is no substitute: a
        /// per-fight skill-use tally reads back at zero once the battle ends.</summary>
        public static string GoalStatus(ActorInstance hero) {
            if (hero == null || hero.RunGoal == null || !hero.GetIsRunGoalComplete(hero.RunGoal)) {
                return null;
            }
            return S.StatusComplete;
        }

        /// <summary>The crossroads' goal offer for a hero, as the hero-select panel words it: the
        /// game's "Goal:" label over the goal's description, then the reward its icon's tooltip
        /// carries (a candle score, a trinket, a rest, a candle item, a loot table, or the
        /// goal's own override text); nothing without a goal.</summary>
        public static IEnumerable<string> GoalOfferLines(ActorInstance hero) {
            var goal = hero?.RunGoal;
            if (goal == null) {
                yield break;
            }
            string description = RunGoalDescription.GetDescription(goal, addCandleBonus: false);
            if (string.IsNullOrEmpty(description)) {
                yield break;
            }
            string label = GameLoc.TryGet("hero_select_objective_label");
            yield return label == null ? description : string.Format(label, description);
            string reward = GoalRewardText(hero, goal);
            if (reward != null) {
                yield return reward;
            }
        }

        // The hero-select panel's own switch over the goal's icon override.
        private static string GoalRewardText(ActorInstance hero, RunGoalDefinition goal) {
            if (!string.IsNullOrEmpty(goal.m_GoalTooltipLocKeyOverride)) {
                return GameLoc.TryGet(goal.m_GoalTooltipLocKeyOverride);
            }
            switch (goal.m_GoalIconOverride) {
                case "trinket": {
                    string loot = hero.GetRunGoalLootDescription();
                    return string.IsNullOrEmpty(loot) ? GameLoc.TryGet("goal_trinket_reward_tooltip") : loot;
                }
                case "rest": {
                    string loot = hero.GetRunGoalLootDescription();
                    return string.IsNullOrEmpty(loot) ? GameLoc.TryGet("goal_hero_rest_reward_tooltip") : loot;
                }
                case "candle_item":
                    return GameLoc.TryGet("goal_" + goal.m_LootTableId + "_reward_tooltip");
            }
            if (!string.IsNullOrEmpty(goal.m_LootTableId)) {
                return GameLoc.TryGet("goal_default_loot_reward_tooltip");
            }
            string candle = GameLoc.TryGet("goal_candle_reward_tooltip");
            return candle == null ? null : string.Format(candle, goal.m_Score);
        }

        /// <summary>The hero's run goal as the game's own rows word it: the goal's progress
        /// flavour text (its description when it has none) and the live progress count
        /// ("Scout a region with a Watchtower (0/1)"); null without a goal.</summary>
        public static string GoalText(ActorInstance hero) {
            if (hero == null || hero.RunGoal == null) {
                return null;
            }
            var goal = hero.RunGoal;
            string text = RunGoalDescription.GetProgressFlavourString(goal);
            if (string.IsNullOrEmpty(text)) {
                text = RunGoalDescription.GetDescription(goal, addCandleBonus: false);
            }
            return text + " " + RunGoalDescription.GetProgressString(goal, hero);
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
