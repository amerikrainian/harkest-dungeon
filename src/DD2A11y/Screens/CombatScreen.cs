using System;
using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Queries;
using Assets.Code.Combat;
using Assets.Code.Combat.Queries;
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
    /// combatant with torch, wave count, round detail, and retreat odds in the buffer; then the
    /// turn order; then the battle goal when the fight has one), the enemy strip, the party
    /// strip (both rank-ordered, full status in each combatant's buffer), then the acting
    /// hero's skill bar (skills, move, pass, retreat). Enter on a skill runs the game's
    /// own pick and flips into target-select - every combatant then reads its validity, and
    /// Enter on one sends the game's own actor-pick to execute. Escape cancels target-select,
    /// else opens the pause menu. Turn changes rebuild the tree; turn lines and battle events
    /// (damage, deaths, enemy actions) are announced and kept in the combat buffer, which
    /// empties when the battle ends.
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

        private readonly Action<string, bool> _speak;
        private CombatBhv _combat;
        private SkillSelectionBhv _skillSelection;
        private BattleInfoUiBhv _battleInfo;

        private Container _root;
        private Container _enemies;
        private Container _party;
        private Container _skills;
        private Container _commands;
        private uint _builtTurnGuid;
        private int _builtCombatants;
        private SkillSelectionBhv.InputState _lastInputState;
        private string _lastTurnLine;

        public CombatScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name => S.ScreenCombat;

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
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList, back: Back);
            var header = new Container(ContainerShape.HorizontalList);
            header.Add(new ReadoutElement(HeaderText, detail: HeaderDetail));
            header.Add(new ReadoutElement(TurnOrderText));
            header.Add(new ReadoutElement(GoalText));
            _root.Add(header);
            _enemies = new Container(ContainerShape.HorizontalList, S.CombatEnemies);
            _root.Add(_enemies);
            _party = new Container(ContainerShape.HorizontalList, S.CrossroadsParty);
            _root.Add(_party);
            _skills = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_tab_skills"));
            _root.Add(_skills);
            _commands = new Container(ContainerShape.HorizontalList);
            _root.Add(_commands);
            Populate();
            return _root;
        }

        public override bool OnUpdate(object target) {
            // Battle events: each line is announced (queued, so act-out narration stacks in
            // order) and appended to the combat buffer.
            var events = CombatEvents.Drain();
            if (events != null) {
                foreach (var line in events) {
                    CombatLog.Append(line);
                    _speak(line, false);
                }
            }

            uint turnGuid = CurrentTurnGuid();
            if (turnGuid != _builtTurnGuid) {
                // A new turn: the skill bar belongs to the new actor and combatant state moved.
                // The turn line is spoken outright - focus can sit anywhere (a strip position
                // survives the rebuild in place), so a focus re-announce would read the wrong
                // thing or nothing. The line is logged when it settles (handoffs pass through a
                // transient nameless actor, which reads as null and speaks nothing).
                Populate();
                string line = HeaderText();
                if (line != null && line != _lastTurnLine) {
                    _lastTurnLine = line;
                    CombatLog.Append(line);
                    _speak(line, false);
                }
            } else if (CombatantCount() != _builtCombatants) {
                Populate();
            }

            // Entering target-select is the response to the player's skill pick; announce it.
            // Falling back to skill-select is covered by the turn flow.
            if (_skillSelection != null && _skillSelection.CurrentInputState != _lastInputState) {
                _lastInputState = _skillSelection.CurrentInputState;
                if (_lastInputState == SkillSelectionBhv.InputState.ACTOR_SELECT) {
                    _speak(S.CombatSelectTarget, true);
                }
            }
            return false;
        }

        // ---- Reads ----

        private uint CurrentTurnGuid()
            => _combat != null && _combat.CurrentBattleState != BattleState.INACTIVE ? _combat.CurrentActorGuid : 0;

        // Null while the turn is mid-handoff (a transient current actor with no name), so no
        // half-empty turn line is ever spoken or logged.
        private string HeaderText() {
            string name = Actors.Name(Actors.Get(CurrentTurnGuid()));
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            return S.CombatHeader(_combat.CurrentRound, name);
        }

        // The live remaining acting order, current actor first (the strip of portraits a
        // sighted player plans around).
        private string TurnOrderText() {
            if (_combat == null || _combat.CurrentBattleState == BattleState.INACTIVE) {
                return null;
            }
            var names = new List<string>();
            foreach (uint guid in QueryTurnOrder.Trigger().m_RemainingTurnOrder) {
                string name = Actors.Name(Actors.Get(guid));
                if (name != null) {
                    names.Add(name);
                }
            }
            return names.Count == 0 ? null : S.CombatTurnOrder(SpokenLine.Join(names.ToArray()));
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

        private IEnumerable<string> HeaderDetail() {
            if (Singleton<GameTypeMgr>.Instance != null && Singleton<GameTypeMgr>.Instance.IsGameTypeStarted) {
                yield return S.CombatTorch((int)Singleton<GameTypeMgr>.Instance.RunValues.GetValue(RunValueType.TORCH));
            }
            var scenario = Scenario();
            if (scenario != null && scenario.TotalNumberOfBattles > 1) {
                yield return S.CombatBattleCount(scenario.CurrentBattleConfigurationIndex + 1, scenario.TotalNumberOfBattles);
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

        // ---- Build ----

        private void Populate() {
            _builtTurnGuid = CurrentTurnGuid();
            PopulateTeam(_enemies, friendly: false);
            PopulateTeam(_party, friendly: true);
            _builtCombatants = CombatantCount();
            PopulateActions();
            _lastInputState = _skillSelection != null
                ? _skillSelection.CurrentInputState : SkillSelectionBhv.InputState.SKILL_SELECT;
        }

        private void PopulateTeam(Container strip, bool friendly) {
            strip.Clear();
            foreach (var guid in TeamGuids(friendly)) {
                strip.Add(new CombatantElement(guid, friendly, _skillSelection));
            }
        }

        private int CombatantCount() {
            int count = 0;
            foreach (var _ in TeamGuids(friendly: false)) {
                count++;
            }
            foreach (var _ in TeamGuids(friendly: true)) {
                count++;
            }
            return count;
        }

        // The living combatants of one side, in rank order (the same filter the game's own
        // character sheet applies to the combat party).
        private static IEnumerable<uint> TeamGuids(bool friendly) {
            var query = QueryTeamActors.Trigger(0, friendly);
            var actors = new List<ActorInstance>();
            foreach (uint guid in query.m_TeamActorGuids) {
                var actor = Actors.Get(guid);
                if (actor == null || actor.ActorDataClass.m_IsBattleComplete
                    || actor.ActorDataClass.ContainsTag("kingdoms_ally")) {
                    continue;
                }
                actors.Add(actor);
            }
            actors.Sort((a, b) => a.TeamPosition.CompareTo(b.TeamPosition));
            foreach (var actor in actors) {
                yield return actor.m_ActorGuid;
            }
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

        // The game's word for the retreat control, from the tooltip's own leading label key
        // (shown as "Retreat:"); the trailing colon is list punctuation, not information.
        private string RetreatLabel() {
            string label = GameLoc.TryGet("retreat_tooltip_label");
            return label == null ? null : label.TrimEnd(':', ' ');
        }

        // Escape: first back out of target-select (the game's own cancel), else the pause menu.
        private void Back() {
            if (_skillSelection != null
                && _skillSelection.CurrentInputState == SkillSelectionBhv.InputState.ACTOR_SELECT) {
                _skillSelection.CancelTargetSelection();
                _speak(S.CombatTargetCancelled, true);
                return;
            }
            SingletonMonoBehaviour<CommonUiBhv>.Instance.TogglePauseMenu();
        }
    }
}
