using System;
using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Boss;
using Assets.Code.Buff;
using Assets.Code.Duration;
using Assets.Code.Item;
using Assets.Code.Library;
using Assets.Code.Locale;
using Assets.Code.Profile;
using Assets.Code.Resist;
using Assets.Code.Run;
using Assets.Code.Skill;
using Assets.Code.Source;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// The model reads behind the inspector screen - the same data the game's academic view
    /// renders, taken straight from the actor: the studied skill list (with the view's own
    /// fog-of-war gate on enemy skills the player has not seen), the resistance grid, hero
    /// conditions, trinkets, and the boss blessing.
    /// </summary>
    public static class Study {
        // ---- Skills ----

        /// <summary>The skills the academic view lists for this actor, in its order (an enemy's
        /// round skills lead), filtered the same way (no move/pass/item, visible only).</summary>
        public static List<ActorDataSkill> SkillsOf(ActorInstance actor) {
            var ids = new List<string>(actor.GetEquippedCombatSkillIds(SkillType.TURN, includeMoveSkill: false, includePassSkill: false));
            if (actor.TeamIndex != 0) {
                var startRound = actor.GetEquippedCombatSkillIds(SkillType.START_ROUND, includeMoveSkill: false, includePassSkill: false);
                for (int i = 0; i < startRound.Count; i++) {
                    ids.Insert(i, startRound[i]);
                }
                var endRound = actor.GetEquippedCombatSkillIds(SkillType.END_ROUND, includeMoveSkill: false, includePassSkill: false);
                for (int i = 0; i < endRound.Count; i++) {
                    ids.Insert(i, endRound[i]);
                }
            }
            var skills = new List<ActorDataSkill>();
            foreach (string id in ids) {
                var skill = Actors.Skill(id);
                if (skill == null || skill.IsPassSkill || skill.IsMoveSkill || skill.IsItemSkill
                    || !skill.m_IsTokenViewVisible) {
                    continue;
                }
                skills.Add(skill);
            }
            return skills;
        }

        /// <summary>The academic view's fog of war: an enemy skill reads as "???" until the
        /// player has seen it used (in any run).</summary>
        public static bool HasSeen(ActorDataSkill skill) {
            if (!SingletonMonoBehaviour<PlayerCollectionMgr>.HasInstance()) {
                return true;
            }
            var save = SingletonMonoBehaviour<PlayerCollectionMgr>.Instance.SaveInstance;
            bool seen = save.HasSeenSkill(skill.Id);
            if (!seen && skill.MatchingSkillIds != null) {
                foreach (string matching in skill.MatchingSkillIds) {
                    seen |= save.HasSeenSkill(matching);
                }
            }
            return seen;
        }

        public static string SkillName(ActorDataSkill skill)
            => HasSeen(skill) ? SkillDescription.GetNameText(skill)
                              : GameLoc.TryGet("combat_alt_view_hidden_skill_title");

        /// <summary>The full study card: the skill card plus the view's extra lines (flavor
        /// description, token ignores, use conditions); a hidden line for unseen enemy skills.
        /// Enemies read the game's own token-view card (tokens and dots) - the full effect
        /// renderer is a player-skill surface whose enemy-only internals read as raw ids.</summary>
        public static IEnumerable<string> SkillLines(ActorDataSkill skill, uint actorGuid) {
            if (!HasSeen(skill)) {
                yield return GameLoc.TryGet("combat_alt_view_hidden_skill_desc");
                yield break;
            }
            var actor = Actors.Get(actorGuid);
            var card = actor != null && actor.TeamIndex != 0
                ? SkillCard.TokenViewLines(skill.Id)
                : SkillCard.Lines(skill.Id, actorGuid);
            foreach (var line in card) {
                yield return line;
            }
            string desc = GameLoc.TryGet("skill_desc_" + skill.Id);
            if (desc != null) {
                foreach (var line in SpokenLine.NonEmptyLines(desc)) {
                    yield return line;
                }
            }
            foreach (var line in SpokenLine.NonEmptyLines(SkillDescription.GetTokenIgnoreDescriptions(skill.TokenIgnores))) {
                yield return line;
            }
            foreach (var line in SpokenLine.NonEmptyLines(SkillDescription.GetTokenIgnoreDescriptions(skill.MultiHitSharedTokenIgnores))) {
                yield return line;
            }
            if (skill.AllConditionDefinitions != null && skill.AllConditionDefinitions.Count > 0) {
                foreach (var line in SpokenLine.NonEmptyLines(
                    Assets.Code.Condition.ConditionDescription.GetAllConditionStrings(skill.AllConditionDefinitions, isSkillCondition: true, string.Empty))) {
                    yield return line;
                }
            }
            if (skill.AnyConditionDefinitions != null && skill.AnyConditionDefinitions.Count > 0) {
                foreach (var line in SpokenLine.NonEmptyLines(
                    Assets.Code.Condition.ConditionDescription.GetAnyConditionStrings(skill.AnyConditionDefinitions, isSkillCondition: true, string.Empty))) {
                    yield return line;
                }
            }
        }

        // ---- Resistances ----

        /// <summary>The resist grid's row ids: the actor's own substats first, then every resist
        /// the game defines (the view shows the full grid for every combatant).</summary>
        public static List<string> ResistIds(ActorInstance actor) {
            var ids = new List<string>(actor.GetSubstatKeys(ActorStatType.RESISTANCE));
            var library = SingletonMonoBehaviour<Library<string, ResistDefinition>>.Instance;
            int count = library.GetNumberOfLibraryElements();
            for (int i = 0; i < count; i++) {
                var definition = library.GetLibraryElementAtIndex(i);
                if (definition != null && !ids.Contains(definition.m_Id)) {
                    ids.Add(definition.m_Id);
                }
            }
            return ids;
        }

        public static string ResistName(string id)
            => GameLoc.TryGet("resistance_" + (id == "death" ? "deaths_door" : id));

        /// <summary>The grid cell: "immune", or the clamped percent; null hides the row (death's
        /// door on a combatant that cannot reach it, the hero-only rows on enemies).</summary>
        public static string ResistValue(ActorInstance actor, string id) {
            var definition = SingletonMonoBehaviour<Library<string, ResistDefinition>>.Instance.GetLibraryElement(id);
            if (definition == null) {
                return null;
            }
            bool hero = actor.TeamIndex == 0;
            if (!hero && (id == "disease" || id == "stress")) {
                return null;
            }
            if (actor.ActorDataClass != null && actor.ActorDataClass.m_ResistAlwaysIds.Contains(id)) {
                return GameLoc.TryGet("combat_immune_resist_label");
            }
            if (definition.IsDeath && !actor.GetCanHaveDeathsDoor()) {
                return null;
            }
            float value = actor.GetClampedStatValue(ActorStatType.RESISTANCE, id);
            if (definition.IsDeath && actor.GetHasDeathsDoorArmor()) {
                value = 1f;
            }
            value = ResistCalculation.Clamp(value, definition, actor);
            return (int)Math.Round(value * 100f) + "%";
        }

        /// <summary>The per-source breakdown the resist tooltip shows (class, trinkets, buffs).</summary>
        public static IEnumerable<string> ResistDetail(ActorInstance actor, string id)
            => SpokenLine.NonEmptyLines(ActorDescription.GetActorStatTypeString(actor, ActorStatType.RESISTANCE, isPercentage: true, id));

        // ---- Hero conditions ----

        /// <summary>The view's conditions block for a hero: class conditions, condition-tagged
        /// buffs, stagecoach effects, and the wound line.</summary>
        public static IEnumerable<string> ConditionLines(ActorInstance actor) {
            var entries = new List<Tuple<BuffDefinition, SourceType, string, int, DurationType, bool>>();
            var external = actor.ActorDataClass == null ? null : actor.ActorDataClass.DataExternalBuffs;
            if (external != null) {
                var unlocks = Assets.Code.Unlock.UnlockUtils.GetGameTypeUnlockContainer(actor.UnlockContainer);
                foreach (var buff in external.GetBuffs()) {
                    if (!buff.GetIsUnlocked(unlocks)) {
                        continue;
                    }
                    bool visible = buff.m_IsVisible;
                    if (buff.GetHasUnlock()) {
                        visible &= buff.Tags.Contains("character_sheet_condition");
                    }
                    if (visible) {
                        entries.Add(Tuple.Create(buff, SourceType.CLASS, actor.ActorDataClass.Id, -1, DurationType.INFINITE, true));
                    }
                }
            }
            foreach (var instance in actor.ReadOnlyBuffContainer.GetInstances()) {
                if (instance.IsCharacterSheetCondition) {
                    entries.Add(Tuple.Create(instance.Definition, instance.SourceType, instance.SourceId,
                        instance.GetDurationAmount(), instance.GetDurationType(), instance.IsViewed()));
                }
            }
            foreach (var source in new[] { SourceType.STAGE_COACH_TROPHY, SourceType.STAGE_COACH_PET,
                                           SourceType.STAGE_COACH_GENERAL, SourceType.STAGE_COACH_FLAME }) {
                foreach (var buff in actor.BuffContainer.GetStageCoachBuffs(source)) {
                    if (buff.m_IsVisible && buff.Tags.Contains("character_sheet_condition")) {
                        entries.Add(Tuple.Create(buff, source,
                            GameLoc.TryGet("stat_source_type_" + source), -1, DurationType.INFINITE, true));
                    }
                }
            }
            if (entries.Count > 0) {
                string gender = actor.ActorDataClass == null ? null : actor.ActorDataClass.m_LocalizationGender;
                foreach (var condition in BuffDescription.GetConditionsStrings(entries, gender, actor.ActorGuid)) {
                    foreach (var line in SpokenLine.NonEmptyLines(condition.Item1)) {
                        yield return line;
                    }
                }
            }
            if (actor.IsWounded) {
                string format = GameLoc.TryGet("character_sheet_condition_wound_label");
                if (format != null) {
                    yield return string.Format(format, (int)Math.Round(actor.WoundPercent * 100f));
                }
            }
        }

        // ---- Blessing / trinkets / status ----

        /// <summary>The boss blessing on an ordained enemy: the flavour line and the modifier's
        /// effect description.</summary>
        public static IEnumerable<string> BlessingLines(ActorInstance actor) {
            if (!actor.IsOrdained) {
                yield break;
            }
            string bossId = null;
            if (SingletonMonoBehaviour<RunBhv>.HasInstance() && SingletonMonoBehaviour<RunBhv>.Instance.RunManager != null
                && SingletonMonoBehaviour<RunBhv>.Instance.RunManager.Boss != null) {
                bossId = SingletonMonoBehaviour<RunBhv>.Instance.RunManager.Boss.m_Id;
            }
            if (bossId != null) {
                foreach (var line in SpokenLine.NonEmptyLines(GameLoc.TryGet("blessed_tooltip_" + bossId + "_flavour"))) {
                    yield return line;
                }
            }
            if (actor.BossModifier != null) {
                foreach (var line in SpokenLine.NonEmptyLines(BossDescription.GetBossModifierDescription(actor.BossModifier))) {
                    yield return line;
                }
            }
        }

        public static IReadOnlyList<IReadOnlyItemInstance> TrinketsOf(ActorInstance actor)
            => actor.GetTrinketInventory().GetValidItems();

        public static IEnumerable<string> ItemLines(IReadOnlyItemInstance item)
            => SpokenLine.NonEmptyLines(ItemDescription.GetDescription(item,
                includeRunStatModification: false, canSell: false, showDiscard: false));

        /// <summary>The buff/debuff rows: the view splits the actor's buffs by tag and renders
        /// each group with the game's combined describer.</summary>
        public static IEnumerable<string> BuffLines(ActorInstance actor, bool debuffs) {
            var container = actor.ReadOnlyBuffContainer;
            if (container == null) {
                yield break;
            }
            var group = new List<BuffInstance>();
            foreach (var instance in container.GetInstances()) {
                if (instance?.Definition == null) {
                    continue;
                }
                bool isDebuff = instance.Definition.Tags != null && instance.Definition.Tags.Contains("debuff");
                bool isBuff = instance.Definition.Tags != null && instance.Definition.Tags.Contains("buff");
                if (debuffs ? isDebuff : isBuff) {
                    group.Add(instance);
                }
            }
            if (group.Count == 0) {
                yield break;
            }
            foreach (var line in SpokenLine.NonEmptyLines(BuffDescription.GetDescription(group, separateSources: false, actor.ActorGuid))) {
                yield return line;
            }
        }
    }
}
