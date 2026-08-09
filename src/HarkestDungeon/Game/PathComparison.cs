using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// The path comparison panel (the crossroads seal overlay and the mastery trainer share the
    /// widget) as buffer lines: the selected path's name, flavour, and effect text read from the
    /// panel's own data context (bound TMP text applies a frame late), then the Rank and Target
    /// coverage pips as skill counts per rank, ascending - each pip's fill is the share of the
    /// hero's equipped skills that can act from / hit that rank under the selected path.
    /// </summary>
    public static class PathComparison {
        private static readonly AccessTools.FieldRef<ActorPathComparisonBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<ActorPathComparisonBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<ActorPathComparisonBhv, uint> ActorGuidField =
            AccessTools.FieldRefAccess<ActorPathComparisonBhv, uint>("m_actorGuid");
        private static readonly AccessTools.FieldRef<ActorPathComparisonBhv, List<GameObject>> LaunchPipsField =
            AccessTools.FieldRefAccess<ActorPathComparisonBhv, List<GameObject>>("m_launchPipsAdded");
        private static readonly AccessTools.FieldRef<ActorPathComparisonBhv, List<GameObject>> TargetPipsField =
            AccessTools.FieldRefAccess<ActorPathComparisonBhv, List<GameObject>>("m_targetPipsAdded");
        private static readonly AccessTools.FieldRef<SkillAveragePositionPipBhv, List<RectTransform>> FillsField =
            AccessTools.FieldRefAccess<SkillAveragePositionPipBhv, List<RectTransform>>("m_fillTransforms");

        public static IEnumerable<string> Lines(ActorPathComparisonBhv comparison) {
            if (comparison == null) {
                yield break;
            }
            var context = ContextField(comparison);
            if (context != null) {
                yield return context.GetStringValue("path_title");
                yield return context.GetStringValue("path_flavour");
                string effects = context.GetStringValue("effect_label");
                if (effects != null) {
                    foreach (var line in effects.Split('\n')) {
                        yield return line;
                    }
                }
            }
            var actor = Actors.Get(ActorGuidField(comparison));
            if (actor == null) {
                yield break;
            }
            int limit = actor.ActorDataClass.m_EquippedCombatSkillLimit;
            // The panel draws launch pips rank 4 down to 1 (hero ranks descend toward the
            // enemy line); spoken rank lists always ascend.
            string launch = PipCounts(LaunchPipsField(comparison), reversed: true, limit);
            if (launch != null) {
                yield return S.PathLaunchSkills(launch);
            }
            string target = PipCounts(TargetPipsField(comparison), reversed: false, limit);
            if (target != null) {
                yield return S.PathTargetSkills(target);
            }
        }

        // A pip's fill is the rank's skill count over the equip limit (clamped only past the
        // limit, which a legal loadout never reaches), so the count reads back exactly.
        private static string PipCounts(List<GameObject> pips, bool reversed, int limit) {
            if (pips == null || pips.Count == 0) {
                return null;
            }
            var parts = new string[pips.Count];
            for (int i = 0; i < pips.Count; i++) {
                var pip = pips[reversed ? pips.Count - 1 - i : i].GetComponent<SkillAveragePositionPipBhv>();
                var fills = FillsField(pip);
                float fill = fills == null || fills.Count == 0 ? 0f : fills[0].localScale.x;
                parts[i] = Mathf.RoundToInt(fill * limit).ToString();
            }
            return SpokenLine.Join(parts);
        }
    }
}
