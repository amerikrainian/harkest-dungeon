using System;
using Assets.Code.Actor;
using Assets.Code.Combat;
using Assets.Code.Combat.Queries;
using Assets.Code.Game;
using Assets.Code.UI.Canvases;
using Assets.Code.UI.Events;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Tooltips;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inspector (the game's academic view): the full dossier on one combatant, opened with
    /// I on the focused combatant (the acting hero when focus is elsewhere) - the same overlay
    /// sighted players hold Alt for, driven through the game's own show event so the camera and
    /// fog of war follow. Layout, top to bottom: the identity line (Enter on a party hero opens
    /// their sheet), the studied skills (unseen enemy skills read as the game's own "???"), hero
    /// conditions, trinkets, the resistance grid, then tokens, dots, buffs and debuffs. A and D
    /// cycle combatants battlefield-order without leaving the view; Escape or I closes it, and
    /// the game force-closes it itself when combat resumes animating.
    /// </summary>
    public sealed class AcademicScreen : GameScreen {
        private static readonly AccessTools.FieldRef<CombatUiBhv, AcademicViewUiBhv> ViewField =
            AccessTools.FieldRefAccess<CombatUiBhv, AcademicViewUiBhv>("m_academicViewBhv");
        private static readonly AccessTools.FieldRef<AcademicViewUiBhv, CombatActorBhv> SelectedField =
            AccessTools.FieldRefAccess<AcademicViewUiBhv, CombatActorBhv>("m_CurrentSelectedActor");
        private static readonly AccessTools.FieldRef<AcademicViewUiBhv, int> SelectedIndexField =
            AccessTools.FieldRefAccess<AcademicViewUiBhv, int>("m_currentSelectedActorIndex");
        private static readonly System.Reflection.MethodInfo SwitchToActorMethod =
            AccessTools.Method(typeof(AcademicViewUiBhv), "SwitchToActorAtIndex");

        private readonly Action<string, bool> _speak;
        private AcademicViewUiBhv _view;
        private Container _root;
        private Container _skills;
        private Container _conditions;
        private Container _trinkets;
        private Container _resists;
        private uint _builtGuid;

        public AcademicScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name => S.ScreenInspector;

        // The combat keys stay live here: I closes the view, A/D cycle, the glances still read.
        private static readonly Core.Input.InputCategory[] CombatCategories =
            { Core.Input.InputCategory.Combat, Core.Input.InputCategory.UI };
        public override Core.Input.InputCategory[] InputCategories => CombatCategories;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.COMBAT
                || !SingletonMonoBehaviour<CombatUiBhv>.HasInstance()
                || !SingletonMonoBehaviour<CombatUiBhv>.Instance.IsAcademicViewActive) {
                _view = null;
                return null;
            }
            _view = ViewField(SingletonMonoBehaviour<CombatUiBhv>.Instance);
            return _view;
        }

        private ActorInstance Actor() {
            var view = _view;
            if (view == null) {
                return null;
            }
            var selected = SelectedField(view);
            return selected == null ? null : Actors.Get(selected.GetActorGuid());
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList, back: Close);
            var header = new Container(ContainerShape.HorizontalList);
            header.Add(new InspectorHeaderElement(Actor));
            _root.Add(header);
            _skills = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_tab_skills"));
            _root.Add(_skills);
            _conditions = new Container(ContainerShape.HorizontalList);
            _conditions.Add(new ReadoutElement(
                () => {
                    var actor = Actor();
                    return actor != null && actor.TeamIndex == 0 ? ConditionsLabel() : null;
                },
                detail: () => Study.ConditionLines(Actor())));
            _root.Add(_conditions);
            _trinkets = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_trinkets_title"));
            _root.Add(_trinkets);
            _resists = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_resistances_title"));
            _root.Add(_resists);
            var status = new Container(ContainerShape.HorizontalList);
            status.Add(StatusReadout(() => S.InspectorTokens, actor => {
                var tokens = Actors.VisibleTokens(actor);
                return tokens.Count > 0
                    ? Core.Text.SpokenLine.NonEmptyLines(TokenTooltipBhv.MakeTooltip(tokens))
                    : System.Linq.Enumerable.Empty<string>();
            }));
            status.Add(StatusReadout(() => S.InspectorDots, actor => {
                var dots = actor.DotContainer?.GetInstances();
                return dots != null && dots.Count > 0
                    ? Core.Text.SpokenLine.NonEmptyLines(DotTooltipBhv.MakeTooltipText(dots, condense: false))
                    : System.Linq.Enumerable.Empty<string>();
            }));
            status.Add(StatusReadout(() => GameLoc.TryGet("buffs_label") ?? S.InspectorBuffs,
                actor => Study.BuffLines(actor, debuffs: false)));
            status.Add(StatusReadout(() => GameLoc.TryGet("debuffs_label") ?? S.InspectorDebuffs,
                actor => Study.BuffLines(actor, debuffs: true)));
            _root.Add(status);
            Populate();
            return _root;
        }

        // A section that exists only while it has content: label empty -> unfocusable, so empty
        // sections vanish from the walk instead of reading as blanks.
        private ReadoutElement StatusReadout(Func<string> name, Func<ActorInstance, System.Collections.Generic.IEnumerable<string>> lines) {
            return new ReadoutElement(
                () => {
                    var actor = Actor();
                    if (actor == null) {
                        return null;
                    }
                    foreach (var _ in lines(actor)) {
                        return name();
                    }
                    return null;
                },
                detail: () => {
                    var actor = Actor();
                    return actor == null ? System.Linq.Enumerable.Empty<string>() : lines(actor);
                });
        }

        private static string ConditionsLabel()
            => GameLoc.TryGet("character_sheet_conditions_title") ?? S.InspectorConditions;

        public override bool OnUpdate(object target) {
            var actor = Actor();
            uint guid = actor == null ? 0 : actor.m_ActorGuid;
            if (guid != _builtGuid && guid != 0) {
                // A and D land here: the view switched combatants in place, so the variable rows
                // rebuild and the new subject is spoken outright (focus keeps its row).
                Populate();
                _speak(Actors.Name(actor), true);
            }
            return false;
        }

        // ---- Build ----

        private void Populate() {
            var actor = Actor();
            _builtGuid = actor == null ? 0 : actor.m_ActorGuid;
            _skills.Clear();
            _trinkets.Clear();
            _resists.Clear();
            if (actor == null) {
                return;
            }
            uint guid = actor.m_ActorGuid;
            foreach (var skill in Study.SkillsOf(actor)) {
                var captured = skill;
                _skills.Add(new ReadoutElement(
                    () => Study.SkillName(captured),
                    () => SkillUsesText(captured),
                    () => Study.SkillLines(captured, guid)));
            }
            foreach (var item in Study.TrinketsOf(actor)) {
                var captured = item;
                _trinkets.Add(new ReadoutElement(
                    () => Assets.Code.Item.ItemDescription.GetTitle(captured.GetItemDefinition()),
                    detail: () => Study.ItemLines(captured)));
            }
            foreach (var id in Study.ResistIds(actor)) {
                var captured = id;
                _resists.Add(new ReadoutElement(
                    () => {
                        var current = Actor();
                        return current != null && Study.ResistValue(current, captured) != null
                            ? Study.ResistName(captured) : null;
                    },
                    () => {
                        var current = Actor();
                        return current == null ? null : Study.ResistValue(current, captured);
                    },
                    () => {
                        var current = Actor();
                        return current == null ? System.Linq.Enumerable.Empty<string>()
                                               : Study.ResistDetail(current, captured);
                    }));
            }
        }

        // Remaining limited uses and cooldown, on the actor's own skills (hero side; enemy
        // skills carry neither).
        private string SkillUsesText(Assets.Code.Skill.ActorDataSkill skill) {
            var actor = Actor();
            if (actor == null || !Study.HasSeen(skill)) {
                return null;
            }
            var parts = new System.Collections.Generic.List<string>();
            if (skill.m_Limit > 0) {
                string format = GameLoc.TryGet("effect_tooltip_skill_limit");
                int uses = actor.GetRemainingSkillLimitUses(skill);
                parts.Add(format == null ? uses.ToString() : string.Format(format, uses));
            }
            int cooldown = actor.GetRemainingSkillCooldown(skill);
            if (cooldown > 0) {
                parts.Add(S.InspectorCooldown(cooldown));
            }
            return parts.Count == 0 ? null : Core.Text.SpokenLine.Join(parts.ToArray());
        }

        // ---- Open / cycle / close (driven from the input layer) ----

        /// <summary>I: open on the focused combatant (the acting hero when focus is elsewhere),
        /// or close when already inspecting.</summary>
        public void Toggle(ScreenRouter router, TraditionalNavigator navigator) {
            if (router.Active == this) {
                Close();
                return;
            }
            if (!(router.Active is CombatScreen)) {
                return;
            }
            uint guid = navigator.Current is CombatantElement combatant ? combatant.Guid
                : SingletonMonoBehaviour<CombatBhv>.HasInstance()
                    ? SingletonMonoBehaviour<CombatBhv>.Instance.CurrentActorGuid : 0;
            var actor = Actors.Get(guid);
            if (actor == null || actor.ActorDataClass == null || !actor.ActorDataClass.m_TokenViewValid) {
                _speak(S.StatusUnavailable, true);
                return;
            }
            var combatActor = QueryGetCombatActorBhv.Trigger(guid).CombatActorBhv;
            if (combatActor == null) {
                Plugin.Log.LogWarning("inspector: no CombatActorBhv for actor " + guid);
                _speak(S.StatusUnavailable, true);
                return;
            }
            // The game's own show path (the touch-hold event): camera routine, fog of war, its
            // gates included. Refusal (mid-animation, an enemy turn) leaves the view inactive.
            EventShowAcademicView.Trigger(combatActor);
            if (!SingletonMonoBehaviour<CombatUiBhv>.Instance.IsAcademicViewActive) {
                _speak(S.StatusUnavailable, true);
            }
        }

        /// <summary>A/D: the game's own combatant cycling, battlefield order, both teams.</summary>
        public void Cycle(ScreenRouter router, int direction) {
            if (router.Active != this || _view == null) {
                return;
            }
            SwitchToActorMethod.Invoke(_view, new object[] { SelectedIndexField(_view) + direction });
        }

        private void Close() {
            if (SingletonMonoBehaviour<CombatUiBhv>.HasInstance()) {
                SingletonMonoBehaviour<CombatUiBhv>.Instance.HideAcademicView();
            }
        }
    }
}
