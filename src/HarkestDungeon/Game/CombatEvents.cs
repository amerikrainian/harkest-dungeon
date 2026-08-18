using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.ActorController;
using Assets.Code.Actor.Events;
using Assets.Code.Affinity.Events;
using Assets.Code.Audio;
using Assets.Code.Bark.Events;
using Assets.Code.Buff;
using Assets.Code.Buff.Events;
using Assets.Code.Combat;
using Assets.Code.Combat.Events;
using Assets.Code.Dot;
using Assets.Code.Dot.Events;
using Assets.Code.Events;
using Assets.Code.Game;
using Assets.Code.Library;
using Assets.Code.Quirk;
using Assets.Code.Quirk.Events;
using Assets.Code.Skill;
using Assets.Code.Skill.Events;
using Assets.Code.Source;
using Assets.Code.Token;
using Assets.Code.Token.Events;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Battle-event lines: game events compose a spoken line the moment they fire (names read
    /// while the state is current, in the turn-order form - a duplicated enemy carries its
    /// rank, "Lost Soul 2 took 3 damage") into a pending queue; the combat screen's pump drains it,
    /// announcing each line and appending it to the combat log. Covered: damage (with crits),
    /// heals, stress, meltdowns, misses and dodges, death's door falls and survivals, deaths,
    /// retreat outcomes, wave starts, the final round, wounds, token/dot/buff/quirk gains and
    /// losses (token conversions included), quirk and dot cures, affinity changes, barks,
    /// objective completions, and what enemies do - never the player's own skill picks.
    /// Display gates mirror the game's own pop-text handlers, so what a sighted player sees
    /// pop is what gets spoken.
    /// </summary>
    public static class CombatEvents {
        private static readonly List<string> _pending = new List<string>();
        private static bool _attached;

        /// <summary>Wired by the runtime at startup; the announcement toggles are read live
        /// per event, so a change in the settings tab applies to the next line.</summary>
        internal static Core.Settings.ModSettings Settings;

        /// <summary>Idempotent; attached eagerly at load - the shared handlers route each
        /// event to <see cref="PartyEvents"/> outside combat, so one listener serves both
        /// sides and neither can double-speak.</summary>
        public static void Attach() {
            if (_attached) {
                return;
            }
            _attached = true;
            EventManager.AddListener<EventActorHealthDamage>(HandleDamage);
            EventManager.AddListener<EventActorHealthHeal>(HandleHeal);
            EventManager.AddListener<EventActorDeath>(HandleDeath);
            EventManager.AddListener<EventSelectActor>(HandleActorPick);
            EventManager.AddListener<EventTokenAdded>(HandleTokenAdded);
            EventManager.AddListener<EventTokenConsumed>(HandleTokenConsumed);
            EventManager.AddListener<EventTokenNegated>(HandleTokenNegated);
            EventManager.AddListener<EventTokenReplaced>(HandleTokenReplaced);
            EventManager.AddListener<EventTokenRemoved>(HandleTokenRemoved);
            EventManager.AddListener<EventDotAdded>(HandleDotAdded);
            EventManager.AddListener<EventDotRemoved>(HandleDotRemoved);
            EventManager.AddListener<EventBuffAdded>(HandleBuffAdded);
            EventManager.AddListener<EventQuirkAdded>(HandleQuirkAdded);
            EventManager.AddListener<EventQuirkRemoved>(HandleQuirkRemoved);
            EventManager.AddListener<EventActorResist>(HandleResist);
            EventManager.AddListener<EventStressDamage>(HandleStressDamage);
            EventManager.AddListener<EventStressHeal>(HandleStressHeal);
            EventManager.AddListener<EventActorOverstress>(HandleOverstress);
            EventManager.AddListener<EventActorSurviveDeathsDoor>(HandleSurviveDeathsDoor);
            EventManager.AddListener<EventActorWoundApplied>(HandleWound);
            EventManager.AddListener<EventSkillFinalizeResults>(HandleSkillResults);
            EventManager.AddListener<EventBattleRetreat>(HandleRetreat);
            EventManager.AddListener<EventBattleRetreatFailed>(HandleRetreatFailed);
            EventManager.AddListener<EventBattleBegin>(HandleBattleBegin);
            EventManager.AddListener<EventFinalRound>(HandleFinalRound);
            EventManager.AddListener<EventAffinityTickTriggerApplied>(HandleAffinityTick);
            EventManager.AddListener<EventBark>(HandleBark);
            ToastEvents.Attach();
        }

        public static IReadOnlyList<string> Drain() {
            if (_pending.Count == 0) {
                return null;
            }
            var drained = new List<string>(_pending);
            _pending.Clear();
            return drained;
        }

        public static void Clear() => _pending.Clear();

        /// <summary>An already-composed line for the pump to announce (the toast patches feed
        /// through here too).</summary>
        internal static void Enqueue(string line) {
            if (!string.IsNullOrWhiteSpace(line)) {
                _pending.Add(line);
            }
        }

        private static bool InCombat => GameModeMgr.CurrentMode == GameModeType.COMBAT;

        private static void HandleDamage(EventActorHealthDamage evt) {
            if (!InCombat) {
                PartyEvents.HandleDamage(evt);
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            int damage = (int)evt.m_HealthDamage;
            if (name == null || damage <= 0) {
                return;
            }
            if (evt.m_IsCrit) {
                _pending.Add(S.CombatTookDamageCrit(name, damage));
            } else {
                _pending.Add(damage == 1 ? S.CombatTookDamageOne(name) : S.CombatTookDamage(name, damage));
            }
            if (evt.IsEnteringDeathsDoor) {
                _pending.Add(S.CombatDeathsDoor(name));
            }
        }

        // The model event fires once per heal regardless of which of the game's two display
        // paths shows it, so no HasDisplayed gate here - every heal speaks exactly once.
        private static void HandleHeal(EventActorHealthHeal evt) {
            if (!InCombat) {
                PartyEvents.HandleHeal(evt);
                return;
            }
            if (evt.m_SourceType == SourceType.DEBUG) {
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            int amount = (int)System.Math.Ceiling(evt.m_HealthHeal);
            if (name == null || amount <= 0) {
                return;
            }
            _pending.Add(evt.m_IsCrit ? S.CombatHealedCrit(name, amount) : S.CombatHealed(name, amount));
        }

        // The spoken line stands in for the game's visible death presentation, so deaths shown
        // as one speak and deaths that are mere removals stay silent: Detach is the battle-end
        // sweep that clears leftover corpses off a finished team, and None is a capture's
        // teardown of the taken hero. A corpse's own destruction (smashed by a skill, crumbled
        // on its round timer) speaks by the corpse-deaths toggle, judged by the game's own
        // corpse test.
        private static void HandleDeath(EventActorDeath evt) {
            if (!InCombat
                || evt.m_DeathType.m_DeathPresentationType == DeathPresentationType.Detach
                || evt.m_DeathType.m_DeathPresentationType == DeathPresentationType.None) {
                return;
            }
            var dying = Actors.Get(evt.m_DyingActorGuid);
            if (!Settings.CorpseDeaths.Value && dying != null && AudioConditionUtils.IsCorpse(dying)) {
                return;
            }
            string name = Actors.SpokenName(dying) ?? GameLoc.TryGet(evt.m_DyingActorDataId);
            if (name != null) {
                _pending.Add(S.CombatDied(name));
            }
        }

        // A token the library does not define, or defines as hidden, is internal logic state
        // (the same gate the token icons and buffers apply); its id would leak raw into
        // speech ("gained token_logic_temporary"). Shared with the non-combat party events.
        internal static bool IsSpeakableToken(string tokenId) {
            return SingletonMonoBehaviour<Library<string, TokenDefinition>>.Instance
                       .TryGetLibraryElement(tokenId, out var definition)
                && !definition.IsHidden;
        }

        // A token landed on someone ("Audrey gained Weak"), honoring the game's own pop-text
        // visibility gate so hidden or load-restored applications stay silent. The name is the
        // game's own token string (a glyph, spoken through the sprite words) with the game's own
        // count format when stacked.
        private static void HandleTokenAdded(EventTokenAdded evt) {
            if (!InCombat) {
                PartyEvents.HandleTokenAdded(evt);
                return;
            }
            if (!evt.m_IsPopTextValid || !IsSpeakableToken(evt.m_TokenId)) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            string token = TokenNames.Spoken(evt.m_TokenId);
            if (owner == null || string.IsNullOrEmpty(token)) {
                return;
            }
            if (evt.m_AddAmount > 1) {
                string plural = GameLoc.TryGet("token_amount_format_plural");
                if (plural != null) {
                    token = string.Format(plural, token, evt.m_AddAmount);
                }
            }
            _pending.Add(S.CombatGained(owner, token));
        }

        // A token was used up powering its effect ("Dismas spent Block" explains why a hit dealt
        // half damage). Only instant consumes speak, and only for tokens the game itself pops.
        private static void HandleTokenConsumed(EventTokenConsumed evt) {
            if (!InCombat || evt.m_TokenConsumeType != TokenConsumeType.INSTANT
                || !IsSpeakableToken(evt.m_TokenId)) {
                return;
            }
            var definition = SingletonMonoBehaviour<Library<string, TokenDefinition>>.Instance
                .GetLibraryElement(evt.m_TokenId);
            if (!definition.m_ShowConsumePopText) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            string token = TokenNames.Spoken(evt.m_TokenId);
            if (owner != null && !string.IsNullOrEmpty(token)) {
                _pending.Add(S.CombatSpent(owner, token));
            }
        }

        // A token was destroyed by an effect ("Widow lost Stealth").
        private static void HandleTokenNegated(EventTokenNegated evt) {
            if (!InCombat || !evt.m_IsPopTextValid || !IsSpeakableToken(evt.m_NegatedTokenId)) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            string token = TokenNames.Spoken(evt.m_NegatedTokenId);
            if (owner != null && !string.IsNullOrEmpty(token)) {
                _pending.Add(S.CombatLost(owner, token));
            }
        }

        // A token cleared outright by a skill or a used combat item ("Bigby lost Combo" after
        // Solemnity). The game pops no text for a plain removal - only the actor's token
        // strip loses the icon - so this speaks that visible change for the two deliberate
        // sources and stays silent, like the game, for the container sweeps (duration
        // expiry, the battle-end cleanup, a class transform, death).
        private static void HandleTokenRemoved(EventTokenRemoved evt) {
            if (!InCombat || (evt.Source != SourceType.SKILL && evt.Source != SourceType.INVENTORY)
                || !IsSpeakableToken(evt.Token.Id)) {
                return;
            }
            string owner = Actors.SpokenName(evt.Actor);
            string token = TokenNames.Spoken(evt.Token.Id);
            if (owner != null && !string.IsNullOrEmpty(token)) {
                _pending.Add(S.CombatLost(owner, token));
            }
        }

        // A conversion shows as a gain of the token that took the slot - the game's own pop
        // funnels the replacement id into its token-added handler with a count of one, and
        // only its combat listeners hear the event.
        private static void HandleTokenReplaced(EventTokenReplaced evt) {
            if (!InCombat || !evt.m_IsPopTextValid || !IsSpeakableToken(evt.m_ReplaceAddTokenId)) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            string token = TokenNames.Spoken(evt.m_ReplaceAddTokenId);
            if (owner != null && !string.IsNullOrEmpty(token)) {
                _pending.Add(S.CombatGained(owner, token));
            }
        }

        // A damage-over-time landed ("Dismas gained stress").
        private static void HandleDotAdded(EventDotAdded evt) {
            if (!InCombat) {
                PartyEvents.HandleDotAdded(evt);
                return;
            }
            string owner = Actors.SpokenName(evt.m_Actor);
            string dot = evt.m_DotDefinition == null ? null : DotDescription.GetName(evt.m_DotDefinition.m_Type);
            if (owner != null && !string.IsNullOrEmpty(dot)) {
                _pending.Add(S.CombatGained(owner, dot));
            }
        }

        private static void HandleDotRemoved(EventDotRemoved evt) {
            if (!InCombat) {
                PartyEvents.HandleDotRemoved(evt);
                return;
            }
            string line = DotCuredLine(evt);
            if (line != null) {
                _pending.Add(line);
            }
        }

        // The game pops "Cured" only for a skill or trinket cleanse of a dot whose resource
        // wants the text, never over a corpse; natural expiry stays silent. Shared with the
        // non-combat side, whose gate is identical.
        internal static string DotCuredLine(EventDotRemoved evt) {
            if (!evt.Source.m_IsPopTextEligible
                || (evt.Source != SourceType.SKILL && evt.Source != SourceType.TRINKET)) {
                return null;
            }
            var resource = DotResource(evt.Dot);
            if (resource == null || !resource.m_ShowCuredText) {
                return null;
            }
            if (evt.Actor == null || evt.Actor.ContainsTag("corpse")) {
                return null;
            }
            string owner = Actors.SpokenName(evt.Actor);
            string cured = GameLoc.TryGet("pop_text_cured");
            return owner == null || cured == null ? null : SpokenLine.Join(owner, cured);
        }

        // The show-cured flag lives on the dot's resource, reachable only through the pop
        // manager's serialized database.
        private static readonly HarmonyLib.AccessTools.FieldRef<PopTextManager, ResourceDatabaseDots> DotDatabaseField =
            HarmonyLib.AccessTools.FieldRefAccess<PopTextManager, ResourceDatabaseDots>("m_DotResourceDatabase");

        private static ResourceDot DotResource(DotDefinition dot) {
            if (dot == null || !SingletonMonoBehaviour<PopTextManager>.HasInstance()) {
                return null;
            }
            var database = DotDatabaseField(SingletonMonoBehaviour<PopTextManager>.Instance);
            return database == null ? null : database.GetResource(dot.m_Type);
        }

        // A stat buff or debuff landed; the spoken line carries the game's own stat text
        // ("Audrey gained +25% DMG") with the full breakdown living in her combatant buffer.
        private static void HandleBuffAdded(EventBuffAdded evt) {
            if (!InCombat) {
                PartyEvents.HandleBuffAdded(evt);
                return;
            }
            if (!evt.SourceType.m_IsPopTextEligible || !evt.Buff.m_showPopText) {
                return;
            }
            bool isBuff = evt.Buff.IsEligibleToShowAsBuffPopText;
            if (!isBuff && !evt.Buff.IsEligibleToShowAsDebuffPopText) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.TargetActorGuid));
            if (owner == null) {
                return;
            }
            string text = BuffText.Description(evt.Buff);
            if (string.IsNullOrWhiteSpace(text)) {
                text = isBuff ? S.SpriteBuff : S.SpriteDebuff;
            } else {
                text = SpokenLine.Join(", ", text.Split('\n'));
            }
            _pending.Add(S.CombatGained(owner, text));
        }

        // A quirk contracted mid-battle (a meltdown's aftermath); the source gate is the game's
        // own pop-text condition.
        private static void HandleQuirkAdded(EventQuirkAdded evt) {
            if (!InCombat) {
                PartyEvents.HandleQuirkAdded(evt);
                return;
            }
            bool hasSource = !string.IsNullOrEmpty(evt.m_SourceId);
            bool shown = evt.m_SourceType == SourceType.SKILL || evt.m_SourceType == SourceType.RETREAT
                || evt.m_SourceType == SourceType.REST_ITEM || evt.m_SourceType == SourceType.INN
                || evt.m_SourceType == SourceType.STORY || evt.m_SourceType == SourceType.OVERSTRESS
                || (evt.m_SourceType == SourceType.QUIRK && hasSource)
                || (evt.m_SourceType == SourceType.DISEASE && hasSource)
                || (evt.m_SourceType == SourceType.CURSE && hasSource)
                || (evt.m_SourceType == SourceType.TRINKET && hasSource);
            if (!shown) {
                return;
            }
            var actor = Actors.Get(evt.m_ActorGuid);
            string owner = Actors.SpokenName(actor);
            var definition = SingletonMonoBehaviour<Library<string, QuirkDefinition>>.Instance
                .GetLibraryElement(evt.m_QuirkId);
            if (owner == null || definition == null) {
                return;
            }
            _pending.Add(S.CombatGained(owner, QuirkDescription.GetNameString(definition, actor, appendRareIcon: false)));
        }

        // A quirk cured mid-battle pops as the bare "Cured" word; the spoken line adds the
        // hero it floated over.
        private static void HandleQuirkRemoved(EventQuirkRemoved evt) {
            if (!InCombat) {
                PartyEvents.HandleQuirkRemoved(evt);
                return;
            }
            if (!IsCuredQuirkSource(evt.m_Source)
                || SingletonMonoBehaviour<Library<string, QuirkDefinition>>.Instance
                    .GetLibraryElement(evt.m_QuirkId) == null) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            string cured = GameLoc.TryGet("pop_text_cured");
            if (owner != null && cured != null) {
                _pending.Add(SpokenLine.Join(owner, cured));
            }
        }

        // The game's quirk-removal pops fire for these sources only (its combat and
        // non-combat handlers share the filter).
        internal static bool IsCuredQuirkSource(SourceType source) =>
            source == SourceType.REST_ITEM || source == SourceType.TRINKET
            || source == SourceType.SKILL || source == SourceType.COMBAT;

        // An applied effect bounced off ("Woodsman resisted Blight") - without this line, a
        // skill whose rider fails reads as unexplained silence after its damage.
        private static void HandleResist(EventActorResist evt) {
            if (!InCombat) {
                PartyEvents.HandleResist(evt);
                return;
            }
            if (!evt.m_IsPopTextValid) {
                return;
            }
            string owner = Actors.SpokenName(Actors.Get(evt.m_TargetActorGuid));
            if (owner == null) {
                return;
            }
            string what = GameLoc.TryGet("dot_name_" + evt.m_ResistId)
                ?? GameLoc.TryGet("token_name_" + evt.m_ResistId)
                ?? evt.m_ResistId;
            _pending.Add(S.CombatResisted(owner, what));
        }

        private static void HandleStressDamage(EventStressDamage evt) {
            if (!InCombat) {
                PartyEvents.HandleStressDamage(evt);
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            int amount = (int)evt.m_StressDamageAmount;
            if (name != null && amount > 0) {
                _pending.Add(S.CombatStressed(name, amount));
            }
        }

        // Overstress-sourced stress restores are the meltdown's own reset; the meltdown lines
        // already cover that moment.
        private static void HandleStressHeal(EventStressHeal evt) {
            if (!InCombat) {
                PartyEvents.HandleStressHeal(evt);
                return;
            }
            if (evt.m_SourceType == SourceType.OVERSTRESS) {
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            int amount = (int)evt.m_StressHealAmount;
            if (name != null && amount > 0) {
                _pending.Add(S.CombatStressHealed(name, amount));
            }
        }

        // Stress hit its cap: the game's own "resolve is tested" line, then the outcome by the
        // game's name for it ("Dismas gained Meltdown").
        private static void HandleOverstress(EventActorOverstress evt) {
            if (!InCombat || evt.m_OverstressDefinition == null) {
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            if (name == null) {
                return;
            }
            string tested = GameLoc.TryGet("actor_resolve_is_tested_label");
            if (tested != null) {
                _pending.Add(string.Format(tested, name));
            }
            string outcome = GameLoc.TryGet("overstress_" + evt.m_OverstressDefinition.m_Id)
                ?? evt.m_OverstressDefinition.m_Id;
            _pending.Add(S.CombatGained(name, outcome));
        }

        private static void HandleSurviveDeathsDoor(EventActorSurviveDeathsDoor evt) {
            if (!InCombat) {
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            if (name != null) {
                _pending.Add(S.CombatDeathBlowResisted(name));
            }
        }

        private static void HandleWound(EventActorWoundApplied evt) {
            if (!InCombat) {
                PartyEvents.HandleWound(evt);
                return;
            }
            if (!evt.m_SourceType.m_IsPopTextEligible) {
                return;
            }
            string name = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            if (name != null) {
                _pending.Add(evt.m_WoundPercentChange > 0f ? S.CombatWounded(name) : S.CombatWoundHealed(name));
            }
        }

        // The finalized skill outcome carries the whiffs: a miss is the attacker's failure, a
        // dodge the target's save - the same split the game's MISS/DODGE pop text draws. Damage,
        // heals, and riders speak from their own events.
        private static void HandleSkillResults(EventSkillFinalizeResults evt) {
            if (!InCombat || evt.ActorResults == null) {
                return;
            }
            foreach (var result in evt.ActorResults) {
                if (result == null || result.IsHit) {
                    continue;
                }
                string target = Actors.SpokenName(Actors.Get(result.m_TargetActorGuid));
                if (target == null) {
                    continue;
                }
                if (result.IsMiss) {
                    string performer = Actors.SpokenName(Actors.Get(evt.PerformerGuid));
                    if (performer != null) {
                        _pending.Add(S.CombatMissed(performer, target));
                    }
                } else {
                    _pending.Add(S.CombatDodged(target));
                }
            }
        }

        private static void HandleRetreat(EventBattleRetreat evt) {
            if (!InCombat) {
                return;
            }
            Enqueue(GameLoc.TryGet("pop_text_retreat_success"));
        }

        private static void HandleRetreatFailed(EventBattleRetreatFailed evt) {
            if (!InCombat) {
                return;
            }
            Enqueue(GameLoc.TryGet("pop_text_retreat_fail"));
        }

        // A follow-up wave of a chained fight opened ("Battle 2"), with the game's own label.
        private static void HandleBattleBegin(EventBattleBegin evt) {
            if (!InCombat || evt.m_battleIndex <= 0) {
                return;
            }
            string format = GameLoc.TryGet("battle_number_start_label");
            if (format != null) {
                _pending.Add(string.Format(format, evt.m_battleIndex + 1));
            }
        }

        private static void HandleFinalRound(EventFinalRound evt) {
            if (!InCombat) {
                return;
            }
            Enqueue(GameLoc.TryGet("final_round_label"));
        }

        // The relationship meter between two heroes moved ("Dismas and Audrey, affinity +1").
        private static void HandleAffinityTick(EventAffinityTickTriggerApplied evt) {
            if (!InCombat) {
                return;
            }
            string line = Targeting.AffinityLine(evt.m_AffinityTickTriggerInstance);
            if (line != null) {
                _pending.Add(line);
            }
        }

        // A speech bubble ("Dismas: I've had worse odds"); the key is already resolved to the
        // specific line by the game's bark selection.
        private static void HandleBark(EventBark evt) {
            if (!InCombat) {
                return;
            }
            string speaker = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            string text = GameLoc.TryGet(evt.m_BarkKey);
            if (text == null) {
                Plugin.Log.LogWarning($"CombatEvents: bark key \"{evt.m_BarkKey}\" has no localized text");
                return;
            }
            _pending.Add(speaker == null ? text : S.BarkLine(speaker, text));
        }

        // An AI target pick: the same event a player click sends, distinguished by
        // isUserInput=false with an AI-controlled performer holding the turn - the enemy
        // team, but also kingdoms militia allies fighting AI-driven on the party's side.
        // Announces what the AI does; a player-controlled hero's picks stay silent (their
        // outcomes speak instead), including the game's own non-user default-target selects.
        private static void HandleActorPick(EventSelectActor evt) {
            if (!InCombat || evt.m_IsUserInput || !SingletonMonoBehaviour<CombatBhv>.HasInstance()) {
                return;
            }
            var combat = SingletonMonoBehaviour<CombatBhv>.Instance;
            if (combat.CurrentBattleState == BattleState.INACTIVE) {
                return;
            }
            var performer = Actors.Get(combat.CurrentActorGuid);
            if (performer?.Controller == null
                || performer.Controller.m_ActorControllerType == ActorControllerType.INPUT) {
                return;
            }
            string skillId = performer.SelectedSkillId;
            var skill = Actors.Skill(skillId);
            string skillName = skill == null ? null : SkillDescription.GetNameText(skill);
            string target = Actors.SpokenName(Actors.Get(evt.m_ActorGuid));
            if (skillName != null && target != null) {
                _pending.Add(S.CombatUsedSkill(Actors.SpokenName(performer), skillName, target));
            }
        }
    }
}
