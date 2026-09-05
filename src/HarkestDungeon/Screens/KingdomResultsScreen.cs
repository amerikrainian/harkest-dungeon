using System.Collections.Generic;
using Assets.Code.CommonLogic.Pooling;
using Assets.Code.Game;
using Assets.Code.Kingdom.UI;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// A kingdom's end-of-campaign results (the RESULTS mode's surface for a Kingdoms run:
    /// <c>KingdomResultsScreenBhv</c>, a scene canvas with no stack entry, so the floor never
    /// saw it). Named by its own title; reads the outcome's explanation, the kingdom's name
    /// with its difficulty, then the score rows in the floor's own score-row form (days
    /// elapsed, inns destroyed, heroes perished, sieges defeated, militia sacrificed, mastery
    /// collected, upgrades purchased, contracts completed, treasure collected), then Continue,
    /// the screen's own, which returns to the title menu. The presentation reveals the rows
    /// one by one; the entry waits for the last, and a row's arrival only appends its element,
    /// so the stream never re-lands or re-announces.
    /// </summary>
    public sealed class KingdomResultsScreen : GameScreen {
        private static readonly AccessTools.FieldRef<KingdomResultsScreenBhv, TMP_Text> TitleField =
            AccessTools.FieldRefAccess<KingdomResultsScreenBhv, TMP_Text>("m_titleText");
        private static readonly AccessTools.FieldRef<KingdomResultsScreenBhv, TMP_Text> ReasonField =
            AccessTools.FieldRefAccess<KingdomResultsScreenBhv, TMP_Text>("m_reasonText");
        private static readonly AccessTools.FieldRef<KingdomResultsScreenBhv, TMP_Text> MapNameField =
            AccessTools.FieldRefAccess<KingdomResultsScreenBhv, TMP_Text>("m_mapNameText");
        private static readonly AccessTools.FieldRef<KingdomResultsScreenBhv, TMP_Text> DifficultyField =
            AccessTools.FieldRefAccess<KingdomResultsScreenBhv, TMP_Text>("m_difficultyText");
        private static readonly AccessTools.FieldRef<KingdomResultsScreenBhv, List<GameObject>> AddedField =
            AccessTools.FieldRefAccess<KingdomResultsScreenBhv, List<GameObject>>("m_addedScoreObjects");

        private KingdomResultsScreenBhv _results;
        private Container _scores;
        private UIElement _continue;
        private readonly HashSet<int> _added = new HashSet<int>();

        public override string Name {
            get {
                var title = _results == null ? null : TitleField(_results);
                return title == null || string.IsNullOrWhiteSpace(title.text) ? S.ScreenGeneric : title.text;
            }
        }

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.RESULTS) {
                return _results = null;
            }
            var results = Object.FindObjectOfType<KingdomResultsScreenBhv>();
            var canvas = results == null ? null : results.GetComponent<Canvas>();
            _results = canvas != null && canvas.enabled ? results : null;
            return _results;
        }

        // Every row the presentation will show exists from its setup, inactive until its
        // turn; the entry announces once the last one is out.
        public override bool EntrySettled {
            get {
                if (_results == null) {
                    return true;
                }
                var added = AddedField(_results);
                if (added == null) {
                    return true;
                }
                foreach (var row in added) {
                    if (row != null && !row.activeInHierarchy) {
                        return false;
                    }
                }
                return true;
            }
        }

        public override Container BuildRoot(object target) {
            var results = (KingdomResultsScreenBhv)target;
            var root = new RootContainer(ContainerShape.VerticalList);
            root.Add(new StaticTextElement(() => TextOf(ReasonField(results))));
            root.Add(new ReadoutElement(() => TextOf(MapNameField(results)), () => TextOf(DifficultyField(results))));
            _scores = new Container(ContainerShape.VerticalList);
            root.Add(_scores);
            _added.Clear();
            AppendRows(results);
            _continue = new ActionElement(() => GameLoc.TryGet("continue_label") ?? S.StatusComplete, S.RoleButton,
                results.OnContinueButton);
            root.Add(_continue);
            return root;
        }

        public override bool OnUpdate(object target) {
            AppendRows((KingdomResultsScreenBhv)target);
            return false;
        }

        // Rows only ever appear; each new one gets its element appended in place, the rest
        // untouched, so focus and the entry read survive the reveal.
        private void AppendRows(KingdomResultsScreenBhv results) {
            var added = AddedField(results);
            if (added == null) {
                return;
            }
            foreach (var row in added) {
                if (row == null || !row.activeInHierarchy || !_added.Add(row.GetInstanceID())) {
                    continue;
                }
                var score = row.GetComponent<GameOverScoreLabelBhv>();
                var captured = row;
                _scores.Add(score != null ? GenericScreen.ScoreRow(score)
                    : new ReadoutElement(() => captured == null ? null : UiText.AllText(captured)));
            }
        }

        private static string TextOf(TMP_Text text) => text == null || string.IsNullOrWhiteSpace(text.text) ? null : text.text;
    }
}
