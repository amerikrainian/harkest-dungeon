using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Combat;
using Assets.Code.Condition;
using Assets.Code.Skill;
using Assets.Code.Skill.Queries;
using Assets.Code.Token;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Target-side reads for target-select: whether a combatant can take the chosen skill, why
    /// it cannot, and the game's own precomputed per-target preview numbers. The reason walk
    /// mirrors the game's target-validity checks in its exact order; the game itself keeps only
    /// a boolean per target, so the reason is re-derived here from the same model calls.
    /// </summary>
    public static class Targeting {
        /// <summary>The acting hero and their chosen skill while a target pick is pending,
        /// else false.</summary>
        public static bool TryGetPick(out ActorInstance performer, out ActorDataSkill skill) {
            performer = null;
            skill = null;
            if (!SingletonMonoBehaviour<CombatBhv>.HasInstance()) {
                return false;
            }
            var combat = SingletonMonoBehaviour<CombatBhv>.Instance;
            if (combat.CurrentBattleState == BattleState.INACTIVE) {
                return false;
            }
            performer = Actors.Get(combat.CurrentActorGuid);
            if (performer?.Controller == null || performer.SelectedSkillId == null) {
                return false;
            }
            skill = Actors.Skill(performer.SelectedSkillId);
            return skill != null;
        }

        public static bool IsValidTarget(ActorInstance performer, uint targetGuid)
            => performer.Controller.GetIsValidSkillTarget(performer.SelectedSkillId, targetGuid);

        /// <summary>Why the target cannot take the skill, as a terse spoken reason; null when no
        /// check fails (the game may still refuse for a reason its walk keeps private - the low
        /// beep alone carries the state then).</summary>
        public static string InvalidReason(ActorInstance performer, ActorDataSkill skill, ActorInstance target) {
            if (!skill.m_IsAlwaysTargetable && target.ActorDataClass != null && !target.ActorDataClass.m_IsTargetable) {
                return S.TargetUntargetable;
            }
            if (target.GetIsSkillBlocked(skill, isPerformer: false)) {
                return S.TargetBlocked;
            }
            bool self = performer.m_ActorGuid == target.m_ActorGuid;
            bool sameTeam = performer.TeamIndex == target.TeamIndex;
            if (skill.m_IsOnlySelfTargetValid) {
                return self ? null : S.TargetSelfOnly;
            }
            if (skill.m_IsFriendly != sameTeam) {
                return skill.m_IsFriendly ? S.TargetAlliesOnly : S.TargetEnemiesOnly;
            }
            if (!skill.GetHasTargetRank(performer.GetFrontRank(), performer.Size, target.GetFrontRank(), target.Size)
                && !(!skill.m_IsMultiHit
                     && (performer.TokenContainer.GetHasTokenAsPerformer(TokenType.EXTRA_TARGET)
                         || target.TokenContainer.GetHasTokenAsTarget(TokenType.EXTRA_TARGET)))) {
                return S.TargetOutOfRange;
            }
            if (self && sameTeam && !skill.m_IsFriendlySelfTargetValid) {
                return S.TargetNotSelf;
            }
            if (performer.GetIsTargetBlocked(target.m_ActorGuid)) {
                return S.TargetBlocked;
            }
            if (!sameTeam && target.GetHasTeammates()
                && target.TokenContainer.GetHasTokenAsTarget(TokenType.STEALTH)) {
                return S.TargetStealthed;
            }
            var input = new ConditionCalculation.Input(performer, target, skill);
            if (!ConditionCalculation.IsAnyConditionsMet(skill.AnyConditionDefinitions, input)
                || !ConditionCalculation.IsAllConditionsMet(skill.AllConditionDefinitions, input)) {
                return S.TargetConditionNotMet;
            }
            return null;
        }

        /// <summary>The game's precomputed preview against one valid target: hit and crit
        /// chances for attacks, the heal range for friendly skills; null when the game holds no
        /// preview (invalid target, no pick pending).</summary>
        public static string PreviewText(ActorInstance performer, uint targetGuid)
            => PreviewText(performer, performer.SelectedSkillId, targetGuid, terse: false);

        /// <summary>The preview for any of the actor's skills (the game precomputes every valid
        /// skill x target pair at turn start, so no pick needs to be pending). Terse drops the
        /// per-target resist chips and token-removal lists - the T glance reads several targets
        /// in one line, and that tail is read per target after the pick.</summary>
        public static string PreviewText(ActorInstance performer, string skillId, uint targetGuid, bool terse) {
            var query = QuerySkillPreview.Trigger(performer.m_ActorGuid, skillId, targetGuid);
            if (!query.IsValid) {
                return null;
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var preview in query.m_SkillPreviews) {
                if (preview.m_PerformerActorGuid != performer.m_ActorGuid) {
                    continue;
                }
                if (preview.m_ResultType == SkillCalculation.ResultType.TARGET
                    && (preview.m_TargetActorGuid == targetGuid || preview.m_GuardingActorGuid == targetGuid)) {
                    int crit = (int)System.Math.Round(UnityEngine.Mathf.Clamp01(preview.m_CritChance) * 100f);
                    if (preview.m_IsToHitValid) {
                        parts.Add(S.CombatHitChance((int)System.Math.Round(preview.m_ToHitChance * 100f)));
                    }
                    if (preview.m_IsCritValid) {
                        parts.Add(S.CombatCritChance(crit));
                    }
                    // The effective damage the pick would deal - the panel's DMG stat with
                    // every live modifier folded in; a guaranteed crit shows the flat crit
                    // damage, as the game's panel does.
                    if (preview.m_IsDamageValid) {
                        string amount = crit >= 100 ? ((int)preview.m_CritDamage).ToString()
                            : Range((int)preview.m_DamageLow, (int)preview.m_DamageHigh);
                        string word = GameLoc.TryGet("attack_stats_damage");
                        parts.Add(word == null ? amount : amount + " " + word);
                    }
                    if (preview.m_IsHealValid && !preview.m_HideHealPreview) {
                        int low = (int)preview.m_TargetHealthHealBase;
                        int high = low + (int)preview.m_TargetHealthHealRange;
                        parts.Add(S.CombatHealPreview(Range(low, high)));
                    }
                    // A guarded pick: the hit lands on the interceptor, not the focused
                    // combatant - the redirect sighted players see as the preview flashing
                    // over the guardian's bar. Spoken before the removals, which describe
                    // whoever actually takes the hit.
                    if (preview.m_IsToHitValid && preview.m_TargetActorGuid != targetGuid) {
                        parts.Add(S.CombatIntercepted(Actors.Name(Actors.Get(preview.m_TargetActorGuid))));
                    }
                    if (!terse) {
                        AddResistParts(parts, preview);
                        AddRemovalParts(parts, preview);
                    }
                } else if (preview.m_ResultType == SkillCalculation.ResultType.RIPOSTE_TARGET
                           && preview.m_TargetActorGuid == performer.m_ActorGuid
                           && preview.m_IsDamageValid) {
                    parts.Add(S.CombatRiposte(Range((int)preview.m_DamageLow, (int)preview.m_DamageHigh)));
                }
            }
            return parts.Count == 0 ? null : Core.Text.SpokenLine.Join(parts.ToArray());
        }

        // The resistances the pick will test - the chips the game highlights on the enemy
        // panel: the target's value minus the performer's resist-piercing, the panel's own
        // math ("Blight RES 40%" answers whether the dot will stick).
        private static void AddResistParts(List<string> parts,
                SkillCalculation.ActorResult.SkillPreview preview) {
            if (preview.TargetResists == null) {
                return;
            }
            var seen = new List<string>();
            foreach (var resist in preview.TargetResists) {
                if (resist == null || seen.Contains(resist.m_Id)) {
                    continue;
                }
                seen.Add(resist.m_Id);
                string name = Study.ResistName(resist.m_Id);
                if (name == null) {
                    continue;
                }
                preview.TargetResistanceStatValues.TryGetValue(resist.m_Id, out float value);
                preview.PerformerResistanceIgnoreStatValues.TryGetValue(resist.m_Id, out float ignore);
                parts.Add(name + " " + (int)System.Math.Round((value - ignore) * 100f) + "%");
            }
        }

        // What the pick strips off the hit combatant (the preview's real recipient - the
        // guardian on an intercepted pick): the game's own preview lists, the removals it
        // shows by flashing the recipient's tray icons, named only when the recipient holds
        // them - the same gate as the flash. Dot cleanses have no game preview and stay
        // unspoken for parity; the sighted player reads them only in the skill text.
        private static void AddRemovalParts(List<string> parts,
                SkillCalculation.ActorResult.SkillPreview preview) {
            var target = Actors.Get(preview.m_TargetActorGuid);
            if (target == null) {
                return;
            }
            var removed = new List<string>();
            var stolen = new List<string>();
            var converted = new List<string>();
            foreach (var token in Actors.VisibleTokens(target)) {
                string name = TokenDescription.GetNameString(token.Definition.Id);
                if (string.IsNullOrEmpty(name)) {
                    continue;
                }
                if (Contains(preview.TargetTokenRemoveIds, token.Definition.Id)
                    || ContainsAny(preview.TargetTokenRemoveTags, token.Definition.Tags)) {
                    AddName(removed, name);
                }
                if (ContainsAny(preview.TargetTokenStealTags, token.Definition.Tags)) {
                    AddName(stolen, name);
                }
                if (Contains(preview.TargetTokenConvertFromTokenIds, token.Definition.Id)) {
                    AddName(converted, name);
                }
            }
            if (removed.Count > 0) {
                parts.Add(S.CombatRemoves(SpokenLine.Join(removed.ToArray())));
            }
            if (stolen.Count > 0) {
                parts.Add(S.CombatSteals(SpokenLine.Join(stolen.ToArray())));
            }
            if (converted.Count > 0) {
                parts.Add(S.CombatConverts(SpokenLine.Join(converted.ToArray())));
            }
        }

        private static void AddName(List<string> names, string name) {
            if (!names.Contains(name)) {
                names.Add(name);
            }
        }

        // Game-data lists are null when their json field is absent.
        private static bool Contains(IReadOnlyList<string> list, string value) {
            if (list == null || value == null) {
                return false;
            }
            foreach (string entry in list) {
                if (entry == value) {
                    return true;
                }
            }
            return false;
        }

        private static bool ContainsAny(IReadOnlyList<string> list, IReadOnlyList<string> values) {
            if (list == null || values == null) {
                return false;
            }
            foreach (string value in values) {
                if (Contains(list, value)) {
                    return true;
                }
            }
            return false;
        }

        private static string Range(int low, int high) => high > low ? low + "-" + high : low.ToString();
    }
}
