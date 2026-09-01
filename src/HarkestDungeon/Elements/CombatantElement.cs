using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Events;
using Assets.Code.Buff;
using Assets.Code.Combat;
using Assets.Code.Dot;
using Assets.Code.Duration;
using Assets.Code.Token;
using Assets.Code.UI;
using Assets.Code.UI.Events;
using Assets.Code.UI.Tooltips;
using Assets.Code.Utils;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One combatant on the battlefield: name (the turn-order form - a name several living
    /// teammates share carries its rank, "Lost Soul 2"), with rank, HP, and its visible token
    /// stacks ("Combo", "Block x2" - the pips a sighted player sees beside the model) - an
    /// ordained (blessed) one leads with the word, the game's portrait icon ahead of the name. While a skill is
    /// waiting for its target, validity rides as audio (the screen's beeps): an invalid
    /// target's line leads with the reason it cannot be hit, a valid one ends with the game's
    /// own hit/crit/heal preview. Enter sends the game's own actor-pick event (the mouse
    /// click equivalent: executes the selected skill on a valid target, otherwise the game
    /// ignores it); the inspect action opens the hero sheet. The buffer is the full status
    /// readout: an enemy's monster type and speed first (the hover panel's identity facts,
    /// shown nowhere else), the ordainment tooltip when blessed, HP, stress, then one line
    /// per token, dot, and combat buff, all from the game's own describers.
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

        /// <summary>Which side of the battlefield this combatant fights on; the per-team
        /// readers filter the flat battlefield row by it.</summary>
        public bool Friendly => _friendly;

        private ActorInstance Actor => Actors.Get(_guid);

        public override bool CanFocus => Actor != null;

        public override string Label {
            get {
                string name = Actors.SpokenName(Actor);
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
                return SpokenLine.Join(RankText(actor), HpText(actor), TokensText(actor), preview);
            }
        }

        public override string Status => OrdainedWord(Actor);

        // The game marks an ordained combatant with a blessed icon on its portrait and
        // turn-order slot; the word carries that mark at the head of every spoken line.
        private static string OrdainedWord(ActorInstance actor)
            => actor != null && actor.IsOrdained ? S.CombatOrdained : null;

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

        // The game's stress-bar caption; null for monsters, which have no stress bar.
        private string StressText(ActorInstance actor) {
            if (!_friendly) {
                return null;
            }
            string format = GameLoc.TryGet("status_bar_stress");
            return format == null ? null : string.Format(format, (int)actor.Stress, (int)actor.StressMax);
        }

        /// <summary>One line for the battlefield buffers (enemies/party): the glance read with
        /// the rank included, closing with the effects summary - the whole combatant at one
        /// review step.</summary>
        public string OverviewLine() {
            var actor = Actor;
            if (actor == null) {
                return null;
            }
            string preview = PickPending(out var performer, out _) && Targeting.IsValidTarget(performer, _guid)
                ? Targeting.PreviewText(performer, _guid) : null;
            return SpokenLine.Join(OrdainedWord(actor), Label, RankText(actor), HpText(actor),
                StressText(actor), preview, GlanceEffectsLine());
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => _friendly && bufferKey == BufferKeys.Hero
                ? HeroStatus.Lines(_guid) : base.GetSideBufferLines(bufferKey);

        // ---- Glance hotkeys (spoken in place, focus stays put) ----

        /// <summary>The bare-hotkey glance: name, HP, and a hero's stress (the keys map to the
        /// battlefield row's slots left to right); effects belong to the Shift glance.</summary>
        public string GlanceLine() {
            var actor = Actor;
            if (actor == null) {
                return null;
            }
            string preview = PickPending(out var performer, out _) && Targeting.IsValidTarget(performer, _guid)
                ? Targeting.PreviewText(performer, _guid) : null;
            return SpokenLine.Join(OrdainedWord(actor), Label, HpText(actor), StressText(actor), preview);
        }

        /// <summary>The Shift-hotkey glance: every token stack, dot, and combat buff as a
        /// terse line - positives first, then negatives - the token name with its stack count
        /// ("Block x2") and its shown duration when either says more than 1. Null when the
        /// combatant carries none (the caller stays silent).</summary>
        public string GlanceEffectsLine() {
            var actor = Actor;
            if (actor == null) {
                return null;
            }
            var positives = new List<string>();
            var negatives = new List<string>();
            AppendTokenStacks(actor, positives, negatives);
            var dots = actor.DotContainer?.GetInstances();
            if (dots != null && dots.Count > 0) {
                // The game's condensed dot text serves one type at a time (each portrait
                // icon's tooltip holds only its own type): mixed types fed together merge
                // into one line labeled by the first. Group by type and compose per group,
                // healing dots (regen) riding with the positives.
                foreach (var group in DotsByType(dots)) {
                    var side = group[0].Definition.IsHoT ? positives : negatives;
                    foreach (var line in SpokenLine.NonEmptyLines(DotTooltipBhv.MakeTooltipText(group, condense: true))) {
                        side.Add(line);
                    }
                }
            }
            var buffs = actor.BuffContainer?.GetInstances();
            if (buffs != null) {
                foreach (var buff in buffs) {
                    if (buff?.Definition == null || !buff.Definition.IsEligibleToShowAsCombatUi) {
                        continue;
                    }
                    string text = BuffText.Description(buff);
                    if (string.IsNullOrWhiteSpace(text)) {
                        continue;
                    }
                    bool isBuff = buff.Definition.Tags != null && buff.Definition.Tags.Contains("buff");
                    foreach (var line in SpokenLine.NonEmptyLines(text)) {
                        (isBuff ? positives : negatives).Add(line);
                    }
                }
            }
            if (positives.Count == 0 && negatives.Count == 0) {
                return null;
            }
            positives.AddRange(negatives);
            return SpokenLine.Join(positives.ToArray());
        }

        // Each visible token stack as a terse entry - the token name with its stack count
        // ("Block x2") and its shown duration when either says more than 1 - sorted into the
        // caller's positive and negative lists.
        private static void AppendTokenStacks(ActorInstance actor, List<string> positives, List<string> negatives) {
            foreach (var stack in TokenIconBhv.ConvertInstancesToStacks(Actors.VisibleTokens(actor))) {
                var definition = stack.Key.Definition;
                string name = TokenDescription.GetUnglyphedNameString(definition);
                if (stack.Value > 1) {
                    name = S.CombatTokenCount(name, stack.Value);
                }
                int duration = stack.Key.GetDurationAmount();
                if (definition.m_ShowCombatDuration && duration > 1) {
                    name += " (" + DurationDescription.GetDurationText(definition.DurationType, duration) + ")";
                }
                (definition.IsNegative ? negatives : positives).Add(name);
            }
        }

        // The scroll-over form of the token pips: every visible stack, positives first, as one
        // joined run; null when the combatant carries none.
        private static string TokensText(ActorInstance actor) {
            var positives = new List<string>();
            var negatives = new List<string>();
            AppendTokenStacks(actor, positives, negatives);
            positives.AddRange(negatives);
            return positives.Count == 0 ? null : SpokenLine.Join(positives.ToArray());
        }

        // The actor's dots split into one list per dot type, container order both across
        // and within groups.
        private static List<List<DotInstance>> DotsByType(IReadOnlyList<DotInstance> dots) {
            var groups = new List<List<DotInstance>>();
            var byType = new Dictionary<string, List<DotInstance>>();
            foreach (var dot in dots) {
                if (!byType.TryGetValue(dot.Definition.m_Type, out var group)) {
                    group = new List<DotInstance>();
                    byType.Add(dot.Definition.m_Type, group);
                    groups.Add(group);
                }
                group.Add(dot);
            }
            return groups;
        }

        /// <summary>The Ctrl-hotkey glance: the resistance grid as one line, name and value
        /// per resist the game shows for this combatant, the shared RESIST word said once by
        /// its omission ("STUN 20%, MOVE 10%").</summary>
        public string GlanceResistsLine() {
            var actor = Actor;
            if (actor == null) {
                return null;
            }
            var names = new List<string>();
            var values = new List<string>();
            foreach (string id in Study.ResistIds(actor)) {
                string value = Study.ResistValue(actor, id);
                if (value == null) {
                    continue;
                }
                string name = Study.ResistName(id);
                if (name == null) {
                    continue;
                }
                names.Add(name);
                values.Add(value);
            }
            if (names.Count == 0) {
                return null;
            }
            var terse = CommonAffix.Shorten(names);
            var parts = new string[names.Count];
            for (int i = 0; i < parts.Length; i++) {
                parts[i] = terse[i] + " " + values[i];
            }
            return SpokenLine.Join(parts);
        }

        public override IEnumerable<ElementAction> GetActions() {
            // The same event a mouse click on the actor sends; in target-select the battle state
            // machine validates and executes, otherwise it is the game's browse/no-op.
            yield return new ElementAction(ActionIds.Activate,
                () => EventSelectActor.Trigger(_guid, isUserInput: true));
            yield return new ElementAction("inspect", () => EventInspectActor.Trigger(_guid));
        }

        protected override IEnumerable<string> GetDetailLines() {
            var actor = Actor;
            if (actor == null) {
                yield break;
            }
            if (!_friendly) {
                if (actor.ActorDataClass != null) {
                    var tags = new List<string>();
                    foreach (var tag in actor.ActorDataClass.GetPotentialTags()) {
                        string word = GameLoc.TryGet("tag_" + tag);
                        if (word != null && !tags.Contains(word)) {
                            tags.Add(word);
                        }
                    }
                    if (tags.Count > 0) {
                        yield return SpokenLine.Join(tags.ToArray());
                    }
                }
                yield return S.SheetSpeed((int)actor.GetClampedStatValue(ActorStatType.SPEED));
            }
            // The blessed icon's own tooltip: the game's ordainment header, then the
            // modifier's rolled effects.
            if (actor.IsOrdained) {
                string header = GameLoc.TryGet("actor_info_ordained_tooltip_label");
                if (header != null) {
                    yield return header;
                }
                foreach (var line in SpokenLine.NonEmptyLines(
                             Assets.Code.Boss.BossDescription.GetBossModifierDescription(actor.BossModifier))) {
                    yield return line;
                }
            }
            string stress = StressText(actor);
            if (stress != null) {
                yield return stress;
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
                    string text = BuffText.Description(buff);
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
