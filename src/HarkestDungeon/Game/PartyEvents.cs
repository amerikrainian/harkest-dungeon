using Assets.Code.Actor.Events;
using Assets.Code.Buff.Events;
using Assets.Code.Combat.Events;
using Assets.Code.Dot;
using Assets.Code.Dot.Events;
using Assets.Code.Game;
using Assets.Code.Library;
using Assets.Code.Quirk;
using Assets.Code.Quirk.Events;
using Assets.Code.Source;
using Assets.Code.Token.Events;
using Assets.Code.Utils;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Party changes outside combat - a story choice's quirks, stress, heals, tokens and
    /// buffs for the next battle, an inn item's effects - which the game shows only as pop
    /// text over the hero ribbons (its common pop listeners run in every mode). The combat
    /// listeners route each event here when no battle is up, so one listener serves both
    /// sides and neither can double-speak; road collision damage stays with RoadSense. One
    /// line per event, the combat compositions with the non-combat pop handlers' gates,
    /// delivered to the active surface's transient queue: the road's, the inn's, else
    /// straight to queued speech.
    /// </summary>
    public static class PartyEvents {
        /// <summary>Where road-mode lines go (RoadSense's pending queue), wired at load.</summary>
        public static System.Action<string> RoadSink;

        private static void Deliver(string line) {
            if (line == null) {
                return;
            }
            if (GameModeMgr.CurrentMode == GameModeType.DRIVING) {
                RoadSink?.Invoke(line);
            } else if (GameModeMgr.CurrentMode == GameModeType.INN) {
                InnEvents.Enqueue(line);
            } else {
                SpeechPipeline.Instance?.Speak(line, interrupt: false);
            }
        }

        private static string NameOf(uint actorGuid) => Actors.SpokenName(Actors.Get(actorGuid));

        // The same source condition the combat quirk line mirrors from the game's pop text.
        internal static void HandleQuirkAdded(EventQuirkAdded evt) {
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
            if (owner != null && definition != null) {
                Deliver(S.CombatGained(owner, QuirkDescription.GetNameString(definition, actor, appendRareIcon: false)));
            }
        }

        internal static void HandleStressDamage(EventStressDamage evt) {
            string name = NameOf(evt.m_ActorGuid);
            int amount = (int)evt.m_StressDamageAmount;
            if (name != null && amount > 0) {
                Deliver(S.CombatStressed(name, amount));
            }
        }

        internal static void HandleStressHeal(EventStressHeal evt) {
            if (evt.m_SourceType == SourceType.OVERSTRESS) {
                return;
            }
            string name = NameOf(evt.m_ActorGuid);
            int amount = (int)evt.m_StressHealAmount;
            if (name != null && amount > 0) {
                Deliver(S.CombatStressHealed(name, amount));
            }
        }

        // The game's non-combat heal pop skips already-displayed heals and the passive
        // sources (debug, dot ticks, road regen, the hospital's own presentation).
        internal static void HandleHeal(EventActorHealthHeal evt) {
            if (evt.m_HasDisplayed
                || evt.m_SourceType == SourceType.DEBUG || evt.m_SourceType == SourceType.DOT
                || evt.m_SourceType == SourceType.DRIVING || evt.m_SourceType == SourceType.HOSPITAL) {
                return;
            }
            string name = NameOf(evt.m_ActorGuid);
            int amount = (int)System.Math.Ceiling(evt.m_HealthHeal);
            if (name != null && amount > 0) {
                Deliver(evt.m_IsCrit ? S.CombatHealedCrit(name, amount) : S.CombatHealed(name, amount));
            }
        }

        // Road collision damage is RoadSense's; this covers damage on every other non-combat
        // surface (a story's toll, an inn mishap), with the pop handler's source skips.
        internal static void HandleDamage(EventActorHealthDamage evt) {
            if (GameModeMgr.CurrentMode == GameModeType.DRIVING
                || evt.m_SourceType == SourceType.DEBUG || evt.m_SourceType == SourceType.DOT
                || evt.m_SourceType == SourceType.OVERSTRESS) {
                return;
            }
            string name = NameOf(evt.m_ActorGuid);
            int damage = (int)evt.m_HealthDamage;
            if (name == null || damage <= 0) {
                return;
            }
            Deliver(damage == 1 ? S.CombatTookDamageOne(name) : S.CombatTookDamage(name, damage));
        }

        internal static void HandleTokenAdded(EventTokenAdded evt) {
            if (!evt.m_IsPopTextValid || !CombatEvents.IsSpeakableToken(evt.m_TokenId)) {
                return;
            }
            string owner = NameOf(evt.m_ActorGuid);
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
            Deliver(S.CombatGained(owner, token));
        }

        internal static void HandleBuffAdded(EventBuffAdded evt) {
            if (!evt.SourceType.m_IsPopTextEligible || !evt.Buff.m_showPopText) {
                return;
            }
            bool isBuff = evt.Buff.IsEligibleToShowAsBuffPopText;
            if (!isBuff && !evt.Buff.IsEligibleToShowAsDebuffPopText) {
                return;
            }
            string owner = NameOf(evt.TargetActorGuid);
            if (owner == null) {
                return;
            }
            string text = BuffText.Description(evt.Buff);
            if (string.IsNullOrWhiteSpace(text)) {
                text = isBuff ? S.SpriteBuff : S.SpriteDebuff;
            } else {
                text = SpokenLine.Join(", ", text.Split('\n'));
            }
            Deliver(S.CombatGained(owner, text));
        }

        internal static void HandleDotAdded(EventDotAdded evt) {
            string owner = Actors.SpokenName(evt.m_Actor);
            string dot = evt.m_DotDefinition == null ? null : DotDescription.GetName(evt.m_DotDefinition.m_Type);
            if (owner != null && !string.IsNullOrEmpty(dot)) {
                Deliver(S.CombatGained(owner, dot));
            }
        }

        internal static void HandleDotRemoved(EventDotRemoved evt) {
            Deliver(CombatEvents.DotCuredLine(evt));
        }

        // Outside combat the game names a removed regular quirk and pops the bare "Cured"
        // for a curse or disease; the named removal speaks as a loss so it cannot read as a
        // gain.
        internal static void HandleQuirkRemoved(EventQuirkRemoved evt) {
            if (!CombatEvents.IsCuredQuirkSource(evt.m_Source)) {
                return;
            }
            var actor = Actors.Get(evt.m_ActorGuid);
            string owner = Actors.SpokenName(actor);
            var definition = SingletonMonoBehaviour<Library<string, QuirkDefinition>>.Instance
                .GetLibraryElement(evt.m_QuirkId);
            if (owner == null || definition == null) {
                return;
            }
            if (definition.IsCurse || definition.IsDisease) {
                string cured = GameLoc.TryGet("pop_text_cured");
                if (cured != null) {
                    Deliver(SpokenLine.Join(owner, cured));
                }
            } else {
                Deliver(S.CombatLost(owner, QuirkDescription.GetNameString(definition, actor, appendRareIcon: false)));
            }
        }

        internal static void HandleResist(EventActorResist evt) {
            if (!evt.m_IsPopTextValid) {
                return;
            }
            string owner = NameOf(evt.m_TargetActorGuid);
            if (owner == null) {
                return;
            }
            Deliver(S.CombatResisted(owner, CombatEvents.ResistName(evt.m_ResistId)));
        }

        internal static void HandleWound(EventActorWoundApplied evt) {
            if (!evt.m_SourceType.m_IsPopTextEligible) {
                return;
            }
            string name = NameOf(evt.m_ActorGuid);
            if (name != null) {
                Deliver(evt.m_WoundPercentChange > 0f ? S.CombatWounded(name) : S.CombatWoundHealed(name));
            }
        }
    }
}
