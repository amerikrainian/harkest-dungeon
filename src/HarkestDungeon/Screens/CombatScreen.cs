using System;
using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Buff;
using Assets.Code.Combat;
using Assets.Code.Combat.Queries;
using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.Run;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Tooltips;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// A battle (the COMBAT game mode). Layout, top to bottom: the header row (round and acting
    /// combatant with torch value, flame state and effects, wave count, round detail, and
    /// retreat odds in the buffer; then the
    /// turn order; then the battle goal when the fight has one), one battlefield row laid out
    /// like the screen - the party rank 4 to 1, then the enemies rank 1 to 4, full status in
    /// each combatant's buffer - then the acting
    /// hero's skill bar (skills, move, pass, retreat). Enter on a skill runs the game's
    /// own pick and flips into target-select - focus snaps to the first valid target, every
    /// combatant reads its validity, and Enter on one sends the game's own actor-pick to
    /// execute. Escape cancels target-select back onto the picked skill's button, else opens
    /// the pause menu. Battle start resolves the screen before the first turn settles, so the
    /// entry announcement waits for the turn line and lands focus on the header's battle
    /// status - entry reads the round and acting combatant once, never a strip slot plus a
    /// separate turn line. Turn changes and battlefield reorders (a shuffle, a death, a
    /// summon) rebuild the tree, keeping focus on the same combatant across the swap;
    /// turn lines and battle events
    /// (damage, deaths, enemy actions) are announced and kept in the combat buffer, which
    /// empties when the battle ends. The enemies and party buffers carry one overview line per
    /// combatant (<see cref="TeamOverview"/>), a battlefield review that never moves focus.
    /// </summary>
    public sealed class CombatScreen : GameScreen {
        private static readonly AccessTools.FieldRef<SkillSelectionBhv, SkillButtonBhv> MoveButtonField =
            AccessTools.FieldRefAccess<SkillSelectionBhv, SkillButtonBhv>("m_moveButtonBhv");
        private static readonly AccessTools.FieldRef<SkillSelectionBhv, SkillButtonBhv> PassButtonField =
            AccessTools.FieldRefAccess<SkillSelectionBhv, SkillButtonBhv>("m_passButtonBhv");
        private static readonly AccessTools.FieldRef<BattleInfoUiBhv, Button> RetreatButtonField =
            AccessTools.FieldRefAccess<BattleInfoUiBhv, Button>("m_RetreatButton");
        private static readonly AccessTools.FieldRef<BattleInfoUiBhv, TextTooltipBhv> RetreatTooltipField =
            AccessTools.FieldRefAccess<BattleInfoUiBhv, TextTooltipBhv>("m_RetreatTooltip");
        private static readonly AccessTools.FieldRef<BattleInfoUiBhv, TextTooltipBhv> RoundTooltipField =
            AccessTools.FieldRefAccess<BattleInfoUiBhv, TextTooltipBhv>("m_RoundTooltipBhv");
        private static readonly AccessTools.FieldRef<BattleInfoUiBhv, CombatTorchUiBhv> TorchField =
            AccessTools.FieldRefAccess<BattleInfoUiBhv, CombatTorchUiBhv>("m_torchBhv");
        private static readonly AccessTools.FieldRef<BattleInfoUiBhv, Assets.Code.UI.Kingdom.KingdomMapGangEscalationBhv> EscalationField =
            AccessTools.FieldRefAccess<BattleInfoUiBhv, Assets.Code.UI.Kingdom.KingdomMapGangEscalationBhv>("m_gangEscalationBhv");
        private static readonly AccessTools.FieldRef<Assets.Code.UI.Kingdom.KingdomMapGangEscalationBhv, TMPro.TextMeshProUGUI> EscalationTitleField =
            AccessTools.FieldRefAccess<Assets.Code.UI.Kingdom.KingdomMapGangEscalationBhv, TMPro.TextMeshProUGUI>("m_tooltipTitle");
        private static readonly AccessTools.FieldRef<Assets.Code.UI.Kingdom.KingdomMapGangEscalationBhv, TMPro.TextMeshProUGUI> EscalationDescriptionField =
            AccessTools.FieldRefAccess<Assets.Code.UI.Kingdom.KingdomMapGangEscalationBhv, TMPro.TextMeshProUGUI>("m_tooltipDescription");

        private readonly Action<string, bool> _speak;
        private readonly DD2A11y.Core.Audio.IAudioEngine _audio;
        private readonly TraditionalNavigator _navigator;
        private CombatBhv _combat;
        private SkillSelectionBhv _skillSelection;
        private BattleInfoUiBhv _battleInfo;

        private Container _root;
        private ReadoutElement _header;
        private TurnOrderElement _turnOrder;
        private Container _battlefield;
        private Container _skills;
        private Container _commands;
        private uint _builtTurnGuid;
        private List<uint> _builtOrder = new List<uint>();
        private SkillSelectionBhv.InputState _lastInputState;
        private string _lastTurnLine;
        private bool? _lastTargetValid;

        public CombatScreen(Action<string, bool> speak, DD2A11y.Core.Audio.IAudioEngine audio,
                            TraditionalNavigator navigator) {
            _speak = speak;
            _audio = audio;
            _navigator = navigator;
        }

        public override string Name => S.ScreenCombat;

        private static readonly Core.Input.InputCategory[] CombatCategories =
            { Core.Input.InputCategory.Combat, Core.Input.InputCategory.UI };
        public override Core.Input.InputCategory[] InputCategories => CombatCategories;

        // The first turn hands off for a beat after the screen resolves, during which the
        // header reads empty; the entry announcement waits for the turn line.
        public override bool EntrySettled => HeaderText() != null;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.COMBAT || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                Release();
                return null;
            }
            if (!SingletonMonoBehaviour<CombatBhv>.HasInstance()) {
                Release();
                return null;
            }
            _combat = SingletonMonoBehaviour<CombatBhv>.Instance;
            if (_combat.CurrentBattleState == BattleState.INACTIVE) {
                Release();
                return null;
            }
            CombatEvents.Attach();
            if (_skillSelection == null) {
                _skillSelection = UnityEngine.Object.FindObjectOfType<SkillSelectionBhv>();
            }
            if (_battleInfo == null) {
                _battleInfo = UnityEngine.Object.FindObjectOfType<BattleInfoUiBhv>();
            }
            return _combat;
        }

        // The battle is over or gone: the combat buffer empties (it exists only in combat) and
        // the cached widgets drop for the next battle's fresh instances.
        private void Release() {
            if (_combat == null) {
                return;
            }
            _combat = null;
            _skillSelection = null;
            _battleInfo = null;
            CombatLog.Clear();
            CombatEvents.Clear();
            Actors.Numbering.Reset();
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList, back: Back);
            var header = new Container(ContainerShape.HorizontalList);
            _header = new ReadoutElement(HeaderText, detail: HeaderDetail);
            header.Add(_header);
            _turnOrder = new TurnOrderElement(TurnOrderNames);
            header.Add(_turnOrder);
            header.Add(new ReadoutElement(GoalText));
            header.Add(new ReadoutElement(ModifierTitle, detail: ModifierDetail));
            header.Add(new ReadoutElement(EscalationTitle, detail: EscalationDetail));
            _root.Add(header);
            // One battlefield row laid out like the screen: the party right-to-left (rank 4
            // leftmost, rank 1 at the front line), then the enemies rank 1 to 4 continuing
            // rightward - one flat container, so arrows cross the meeting front lines by
            // plain adjacency. The per-team readers (buffers, glances) filter it by each
            // element's side; no strip carries a name - the position IS the side.
            _battlefield = new Container(ContainerShape.HorizontalList);
            _root.Add(_battlefield);
            _skills = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_tab_skills"));
            _root.Add(_skills);
            _commands = new Container(ContainerShape.HorizontalList);
            _root.Add(_commands);
            Populate();
            return _root;
        }

        public override bool OnUpdate(object target) {
            // Battle events: each line is announced (queued, so act-out narration stacks in
            // order) and appended to the combat buffer. Drained only once the entry
            // announcement is out - its interrupt would cut off any event line spoken under
            // the held entry.
            if (EntryAnnounced) {
                var events = CombatEvents.Drain();
                if (events != null) {
                    foreach (var line in events) {
                        CombatLog.Append(line);
                        _speak(line, false);
                    }
                }
            }

            uint turnGuid = CurrentTurnGuid();
            if (turnGuid != _builtTurnGuid) {
                // A new turn: the skill bar belongs to the new actor and combatant state moved.
                Repopulate();
                RecordTurnLine();
            } else if (RowOrderChanged()) {
                // Combatants move without a turn or count change (shuffle trinkets at battle
                // start, mid-turn repositioning), and deaths and summons change the roster;
                // the row identity check catches both, so the glances and team buffers that
                // read the row in built order never answer with a stale arrangement.
                Repopulate();
            }

            if (!EntryAnnounced && HeaderText() != null) {
                // The first turn settled under the held entry: focus moves to the header's
                // battle status so the announcement the router now releases reads
                // "combat, round N, name" - the turn line's one utterance.
                RecordTurnLine();
                _navigator.Focus(_header, announce: false);
            }

            // Entering target-select is the response to the player's skill pick: focus snaps
            // to the first combatant the pick can take, so targeting starts on a target
            // instead of the skill bar (the landing line carries the preview, the validity
            // beep rides the settle, and arrows browse on from there). Falling back to
            // skill-select is covered by the turn flow and the Escape path.
            if (_skillSelection != null && _skillSelection.CurrentInputState != _lastInputState) {
                _lastInputState = _skillSelection.CurrentInputState;
                _lastTargetValid = null;
                if (_lastInputState == SkillSelectionBhv.InputState.ACTOR_SELECT) {
                    FocusFirstValidTarget();
                }
            }
            return false;
        }

        // Target validity rides as audio while a pick is pending: a high beep on landing on a
        // valid target, a low one on an invalid target, and only when the validity CHANGED from
        // the previously focused combatant - a run of same-validity targets stays silent.
        public void OnFocusSettled(UIElement element) {
            if (_skillSelection == null
                || _skillSelection.CurrentInputState != SkillSelectionBhv.InputState.ACTOR_SELECT
                || !(element is CombatantElement combatant)
                || !Game.Targeting.TryGetPick(out var performer, out _)) {
                return;
            }
            bool valid = Game.Targeting.IsValidTarget(performer, combatant.Guid);
            if (_lastTargetValid == valid) {
                return;
            }
            _lastTargetValid = valid;
            _audio.PlayCue(valid ? DD2A11y.Core.Audio.AudioCue.CombatTargetValid
                                 : DD2A11y.Core.Audio.AudioCue.CombatTargetInvalid, 1f, 0f);
        }

        // A settled turn line is logged to the combat buffer and spoken outright - focus can
        // sit anywhere, so a focus re-announce would read the wrong thing or nothing.
        // Handoffs pass through a
        // transient nameless actor, which reads as null and records nothing. Under a held
        // entry the line is logged but stays unspoken: the entry announcement carries it.
        private void RecordTurnLine() {
            string line = HeaderText();
            if (line == null || line == _lastTurnLine) {
                return;
            }
            _lastTurnLine = line;
            CombatLog.Append(line);
            if (EntryAnnounced) {
                _speak(line, false);
            }
        }

        // ---- Reads ----

        private uint CurrentTurnGuid()
            => _combat != null && _combat.CurrentBattleState != BattleState.INACTIVE ? _combat.CurrentActorGuid : 0;

        // Null while the turn is mid-handoff (a transient current actor with no name), so no
        // half-empty turn line is ever spoken or logged.
        private string HeaderText() {
            string name = Actors.SpokenName(Actors.Get(CurrentTurnGuid()));
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            return S.CombatHeader(_combat.CurrentRound, name);
        }

        // The live remaining acting order, current actor first (the strip of portraits a
        // sighted player plans around); empty outside an active battle.
        private List<string> TurnOrderNames() {
            var names = new List<string>();
            if (_combat == null || _combat.CurrentBattleState == BattleState.INACTIVE) {
                return names;
            }
            foreach (uint guid in QueryTurnOrder.Trigger().m_RemainingTurnOrder) {
                string name = Actors.SpokenName(Actors.Get(guid));
                if (name != null) {
                    names.Add(name);
                }
            }
            return names;
        }

        // The battle's objective, in fights that carry one (kingdoms defenses); absent
        // otherwise, which hides the element.
        private string GoalText() {
            var config = Scenario()?.CurrentBattleConfiguration;
            return config == null ? null : GameLoc.TryGet("battle_goal_" + config.m_Id);
        }

        private static CombatScenarioData Scenario() {
            var gameType = Singleton<GameTypeMgr>.Instance;
            return gameType == null ? null : gameType.CombatScenarioData;
        }

        // The battle's rolled modifier, in fights that carry one; absent otherwise, which hides
        // the element. Title is the game's own short name, the buffer holds its tooltip title
        // and effect/buff descriptions - the same describers the visual tooltip renders.
        private string ModifierTitle() {
            var modifier = _combat == null ? null : _combat.CurrentBattleModifier;
            return modifier == null ? null : GameLoc.TryGet("battle_modifier_title_" + modifier.m_Id);
        }

        private IEnumerable<string> ModifierDetail() {
            var modifier = _combat == null ? null : _combat.CurrentBattleModifier;
            if (modifier == null) {
                yield break;
            }
            string title = GameLoc.TryGet("battle_modifier_tooltip_title_" + modifier.m_Id);
            if (title != null) {
                yield return title;
            }
            if (modifier.ActorDataSkillEffects != null) {
                foreach (var line in SpokenLine.NonEmptyLines(
                    ActorDataEffectDescription.GetDescription(modifier.ActorDataSkillEffects, null, addLineOnEffect: false))) {
                    yield return line;
                }
            }
            if (modifier.ActorDataExternalBuffs != null) {
                foreach (var buff in modifier.ActorDataExternalBuffs.GetBuffs()) {
                    foreach (var line in SpokenLine.NonEmptyLines(
                        BuffText.Description(buff))) {
                        yield return line;
                    }
                }
            }
        }

        private IEnumerable<string> HeaderDetail() {
            if (Singleton<GameTypeMgr>.Instance != null && Singleton<GameTypeMgr>.Instance.IsGameTypeStarted) {
                yield return S.CombatTorch((int)Singleton<GameTypeMgr>.Instance.RunValues.GetValue(RunValueType.TORCH));
                foreach (var line in FlameDetail()) {
                    yield return line;
                }
            }
            // The wave readout mirrors the game's pip strip beside the round counter
            // (BattleInfoUiBhv.AttemptToSetCombatMap): an enemy summon controller configured
            // to show wave progress reads as two stages - the second still ahead while its
            // wave queue holds - else the scenario's chained battles. A single battle shows
            // no pips and speaks no line.
            var waves = QuerySummonController.Query(1);
            if (waves.m_ShowWaveProgress) {
                yield return S.CombatBattleCount(waves.m_HasWaveQueue ? 1 : 2, 2);
            } else {
                var scenario = Scenario();
                if (scenario != null && scenario.TotalNumberOfBattles > 1) {
                    yield return S.CombatBattleCount(scenario.CurrentBattleConfigurationIndex + 1, scenario.TotalNumberOfBattles);
                }
            }
            if (_battleInfo == null) {
                yield break;
            }
            foreach (var line in TooltipReader.LinesOf(RoundTooltipField(_battleInfo))) {
                yield return line;
            }
            foreach (var line in TooltipReader.LinesOf(RetreatTooltipField(_battleInfo))) {
                yield return line;
            }
            var torch = TorchField(_battleInfo);
            if (torch != null) {
                foreach (var line in TooltipReader.Lines(torch.gameObject)) {
                    yield return line;
                }
            }
        }

        // The flame's hover panel: the state name, then each side's current effects under the
        // game's own Heroes/Enemies labels. The panel itself activates only on mouse hover,
        // but the widget keeps its data-bound values current on every flame change, so the
        // lines read from those. A mid flame grants neither side anything and reads only the
        // state name.
        private IEnumerable<string> FlameDetail() {
            var torch = _battleInfo == null ? null : TorchField(_battleInfo);
            var context = torch == null ? null : torch.GetComponent<DataContextBhv>();
            if (context == null) {
                yield break;
            }
            yield return context.GetStringValue("torch_title");
            yield return FlameSide("party_heroes_label", context.GetStringValue("torch_effects_heroes"));
            yield return FlameSide("party_enemies_label", context.GetStringValue("torch_effects_enemies"));
        }

        private static string FlameSide(string labelKey, string effects) {
            string joined = SpokenLine.Join(", ", SpokenLine.NonEmptyLines(effects));
            if (joined.Length == 0) {
                return null;
            }
            return SpokenLine.Join(", ", new[] { GameLoc.TryGet(labelKey), joined });
        }

        // The Kingdoms gang-escalation ribbon, its own header stop like the battle modifier;
        // absent outside Kingdoms combat, which hides the element. The game composes the
        // tooltip title ("Escalation 2") and effect lines into the widget's TMPs once at
        // battle start, so the text is the game's own and safe to read. Sighted access is
        // the More Info hold.
        private string EscalationTitle() {
            var escalation = _battleInfo == null ? null : EscalationField(_battleInfo);
            if (escalation == null || !escalation.gameObject.activeSelf) {
                return null;
            }
            var title = EscalationTitleField(escalation);
            return title == null ? null : title.text;
        }

        private IEnumerable<string> EscalationDetail() {
            var escalation = _battleInfo == null ? null : EscalationField(_battleInfo);
            var description = escalation == null ? null : EscalationDescriptionField(escalation);
            if (description == null) {
                yield break;
            }
            foreach (var line in description.text.Split('\n')) {
                yield return line;
            }
        }

        // ---- Build ----

        // A rebuild replaces every row element; when focus sat on a combatant, re-land
        // silently on its replacement so the cursor keeps its place (orphan recovery would
        // drop it at the row's left end and announce the landing).
        private void Repopulate() {
            var focused = _navigator.Current as CombatantElement;
            Populate();
            if (focused == null) {
                return;
            }
            var landed = FindCombatant(focused.Guid);
            if (landed != null && landed.CanFocus) {
                _navigator.Focus(landed, announce: false);
            }
        }

        private void Populate() {
            _builtTurnGuid = CurrentTurnGuid();
            PopulateBattlefield();
            _builtOrder = RowOrder();
            PopulateActions();
            _lastInputState = _skillSelection != null
                ? _skillSelection.CurrentInputState : SkillSelectionBhv.InputState.SKILL_SELECT;
            _lastTargetValid = null;
        }

        private void PopulateBattlefield() {
            _battlefield.Clear();
            // The party mirrors the sighted layout, rank 4 leftmost; everything that reads
            // battlefield children in order (the team buffers, the QWER glances) follows
            // this left-to-right order.
            var party = Actors.Team(friendly: true);
            party.Reverse();
            foreach (var actor in party) {
                _battlefield.Add(new CombatantElement(actor.m_ActorGuid, friendly: true, _skillSelection));
            }
            foreach (var actor in Actors.Team(friendly: false)) {
                _battlefield.Add(new CombatantElement(actor.m_ActorGuid, friendly: false, _skillSelection));
            }
        }

        // One side of the battlefield in row order, filtered from the flat container.
        private IEnumerable<CombatantElement> Side(bool friendly) {
            foreach (var child in _battlefield.Children) {
                if (child is CombatantElement combatant && combatant.Friendly == friendly) {
                    yield return combatant;
                }
            }
        }

        /// <summary>The enemies/party buffer source: one overview line per combatant in rank
        /// order, filtered by side from the battlefield row. Empty outside a battle, which hides the buffer
        /// from the review keys.</summary>
        public IEnumerable<string> TeamOverview(bool friendly) {
            if (_combat == null || _battlefield == null) {
                yield break;
            }
            foreach (var combatant in Side(friendly)) {
                if (combatant.CanFocus) {
                    string line = combatant.OverviewLine();
                    if (line != null) {
                        yield return line;
                    }
                }
            }
        }

        // The ordered guids the battlefield row would be built from right now, in the row's
        // own layout (party rank 4 to 1, then enemies rank 1 to 4).
        private static List<uint> RowOrder() {
            var order = new List<uint>();
            var party = Actors.Team(friendly: true);
            for (int i = party.Count - 1; i >= 0; i--) {
                order.Add(party[i].m_ActorGuid);
            }
            foreach (var actor in Actors.Team(friendly: false)) {
                order.Add(actor.m_ActorGuid);
            }
            return order;
        }

        private bool RowOrderChanged() {
            var order = RowOrder();
            if (order.Count != _builtOrder.Count) {
                return true;
            }
            for (int i = 0; i < order.Count; i++) {
                if (order[i] != _builtOrder[i]) {
                    return true;
                }
            }
            return false;
        }

        private void PopulateActions() {
            _skills.Clear();
            _commands.Clear();
            if (_skillSelection != null) {
                for (int i = 0; i < _skillSelection.SkillButtonCount; i++) {
                    _skills.Add(new CombatSkillElement(_skillSelection.GetSkillButton(i), _skillSelection, i));
                }
                _commands.Add(new CombatSkillElement(MoveButtonField(_skillSelection)));
                _commands.Add(new CombatSkillElement(PassButtonField(_skillSelection)));
            }
            if (_battleInfo != null) {
                var retreatButton = RetreatButtonField(_battleInfo);
                if (retreatButton != null && retreatButton.gameObject.activeInHierarchy) {
                    var battleInfo = _battleInfo;
                    _commands.Add(new ActionElement(RetreatLabel, S.RoleButton,
                        battleInfo.ButtonFuncAttemptRetreat,
                        () => TooltipReader.LinesOf(RetreatTooltipField(battleInfo))));
                }
            }
        }

        // ---- Glance hotkeys ----

        // The combatant hotkeys speak one side's slot left to right - the battlefield row's
        // own order - without moving focus: the digit row glances enemies (1 = their rank
        // 1), the QWER row the party (Q = the backmost, R = the front line). A slot with no
        // combatant is silent - the empty slot is the answer.

        public void GlanceStatus(bool friendly, int index)
            => Glance(friendly, index, e => e.GlanceLine());

        public void GlanceEffects(bool friendly, int index)
            => Glance(friendly, index, e => e.GlanceEffectsLine());

        public void GlanceResists(bool friendly, int index)
            => Glance(friendly, index, e => e.GlanceResistsLine());

        private void Glance(bool friendly, int index, Func<CombatantElement, string> line) {
            if (_battlefield == null) {
                return;
            }
            CombatantElement element = null;
            int slot = 0;
            foreach (var combatant in Side(friendly)) {
                if (slot++ == index) {
                    element = combatant;
                    break;
                }
            }
            if (element == null || !element.CanFocus) {
                return;
            }
            string text = line(element);
            if (text != null) {
                _speak(text, true);
            }
        }

        /// <summary>S: the acting combatant's glance, without hunting for its strip key.</summary>
        public void GlanceActor() {
            var element = FindCombatant(CurrentTurnGuid());
            string line = element != null && element.CanFocus ? element.GlanceLine() : null;
            if (line != null) {
                _speak(line, true);
            }
        }

        /// <summary>Shift+T: the header's turn-order line from anywhere in the battle.</summary>
        public void GlanceTurnOrder() {
            string line = _turnOrder.Label;
            if (line != null) {
                _speak(line, true);
            }
        }

        /// <summary>T on a focused skill: every combatant the skill could take right now, each
        /// with its terse preview - the game's own precomputed valid-target entries, read
        /// without picking. On a skill with no valid use it speaks the skill's grey reason;
        /// off the skill bar the key is silent.</summary>
        public void GlanceTargets() {
            if (!(_navigator.Current is CombatSkillElement skill)) {
                return;
            }
            var performer = Actors.Get(skill.ActorGuid);
            var targets = Game.Targeting.ValidTargets(performer, skill.SkillId);
            var parts = new List<string>();
            if (targets != null && targets.Count > 0) {
                foreach (bool friendly in new[] { false, true }) {
                    foreach (var combatant in Side(friendly)) {
                        if (ContainsGuid(targets, combatant.Guid)) {
                            parts.Add(SpokenLine.Join(
                                Actors.SpokenName(Actors.Get(combatant.Guid)),
                                Game.Targeting.PreviewText(performer, skill.SkillId, combatant.Guid, terse: true)));
                        }
                    }
                }
            }
            if (parts.Count > 0) {
                _speak(string.Join("; ", parts), true);
                return;
            }
            string reason = skill.InvalidReasonText();
            if (reason != null) {
                _speak(reason, true);
            }
        }

        /// <summary>A: the telegraphed affinity changes - on a focused skill every valid
        /// target's, on a focused combatant while a pick is pending the pick's change against
        /// them (the icon a sighted player sees on the responding hero when hovering). A skill
        /// or target that telegraphs nothing answers with silence. The chord is shared with
        /// the inspector's combatant cycling; the focus gates keep exactly one of the two
        /// live at a time.</summary>
        public void GlanceAffinity() {
            string text = null;
            if (_navigator.Current is CombatSkillElement skill) {
                var performer = Actors.Get(skill.ActorGuid);
                if (performer != null) {
                    text = SpokenLine.Join("; ",
                        Game.Targeting.AffinityPreviews(performer, skill.SkillId));
                }
            } else if (_navigator.Current is CombatantElement combatant
                       && Game.Targeting.TryGetPick(out var performer2, out _)) {
                text = Game.Targeting.AffinityPreview(
                    performer2, performer2.SelectedSkillId, combatant.Guid);
            }
            if (!string.IsNullOrEmpty(text)) {
                _speak(text, true);
            }
        }

        private CombatantElement FindCombatant(uint guid) {
            foreach (var child in _battlefield.Children) {
                if (child is CombatantElement combatant && combatant.Guid == guid) {
                    return combatant;
                }
            }
            return null;
        }

        private static bool ContainsGuid(IReadOnlyList<uint> guids, uint guid) {
            for (int i = 0; i < guids.Count; i++) {
                if (guids[i] == guid) {
                    return true;
                }
            }
            return false;
        }

        // The game's word for the retreat control, from the tooltip's own leading label key
        // (shown as "Retreat:"); the trailing colon is list punctuation, not information.
        private string RetreatLabel() {
            string label = GameLoc.TryGet("retreat_tooltip_label");
            return label == null ? null : label.TrimEnd(':', ' ');
        }

        private void FocusFirstValidTarget() {
            if (!Game.Targeting.TryGetPick(out var performer, out _)) {
                return;
            }
            // Enemies first, so a hostile pick snaps to an enemy and a friendly one to the
            // party.
            foreach (bool friendly in new[] { false, true }) {
                foreach (var combatant in Side(friendly)) {
                    if (combatant.CanFocus && Game.Targeting.IsValidTarget(performer, combatant.Guid)) {
                        _navigator.Focus(combatant, announce: true);
                        return;
                    }
                }
            }
        }

        // Escape: first back out of target-select, landing back on the picked skill's
        // button, whose plain line ("Crush, button") is the whole feedback; else the pause
        // menu. The pick is fully deselected via the game's own deselect event (its END_TURN
        // path) - the bare CancelTargetSelection keeps the actor's selected skill armed for
        // the mouse flow, which leaves the button's OnClick refusing a re-pick of the same
        // skill. The deselect must fire BEFORE the cancel: the skill bar's own listener
        // flips back into target-select on any selection event naming one of its buttons.
        private void Back() {
            if (_skillSelection != null
                && _skillSelection.CurrentInputState == SkillSelectionBhv.InputState.ACTOR_SELECT) {
                var skill = SelectedSkillElement();
                if (Game.Targeting.TryGetPick(out var performer, out _)) {
                    Assets.Code.Actor.Events.EventSkillSelectionChanged.Trigger(isSelected: false,
                        performer.m_ActorGuid, performer.SelectedSkillId,
                        isUserInput: false, autohighlightTarget: false);
                }
                _skillSelection.CancelTargetSelection();
                if (skill != null) {
                    _navigator.Focus(skill, announce: true);
                } else {
                    Plugin.Log.LogWarning("combat: no bar button for the cancelled pick");
                }
                return;
            }
            SingletonMonoBehaviour<CommonUiBhv>.Instance.TogglePauseMenu();
        }

        private CombatSkillElement SelectedSkillElement() {
            if (!Game.Targeting.TryGetPick(out var performer, out _)) {
                return null;
            }
            foreach (var strip in new[] { _skills, _commands }) {
                foreach (var child in strip.Children) {
                    if (child is CombatSkillElement element && element.CanFocus
                        && element.SkillId == performer.SelectedSkillId) {
                        return element;
                    }
                }
            }
            return null;
        }
    }
}
