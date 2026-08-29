using System.Collections.Generic;
using Assets.Code.UI.Tooltips;
using HarmonyLib;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// Collects the tooltip text a widget carries WITHOUT showing the tooltip: the text-bearing
    /// tooltip components hold their source data on the widget itself (a loc key, a populated
    /// string). One line per tooltip; the buffer system presents them under the focused element.
    /// A skill tooltip's held text is only its header stats; the full skill detail is composed
    /// from the model by the screens that show skills.
    /// </summary>
    public static class TooltipReader {
        private static readonly AccessTools.FieldRef<LocalizedTextTooltipBhv, string> LocKeyField =
            AccessTools.FieldRefAccess<LocalizedTextTooltipBhv, string>("m_locKey");
        private static readonly AccessTools.FieldRef<TextTooltipBhv, string> TextField =
            AccessTools.FieldRefAccess<TextTooltipBhv, string>("m_text");

        // Nearly every remaining tooltip type (item, quirk, trinket, token, buff...) holds its
        // populated text in a private "m_text" of its own; resolved per concrete type, with a
        // one-time log for a type that lacks it (so a game reshape is visible, not silent).
        private static readonly Dictionary<System.Type, System.Reflection.FieldInfo> TextFields =
            new Dictionary<System.Type, System.Reflection.FieldInfo>();

        /// <summary>One line per readable tooltip within <paramref name="scope"/> (active objects,
        /// enabled tooltip components only - the game disables a component to suppress its
        /// tooltip).</summary>
        public static IEnumerable<string> Lines(GameObject scope) {
            if (scope == null) {
                yield break;
            }
            foreach (var tooltip in scope.GetComponentsInChildren<TooltipUiBhv>(includeInactive: false)) {
                if (!tooltip.enabled) {
                    continue;
                }
                foreach (var line in LinesOf(tooltip)) {
                    yield return line;
                }
            }
        }

        /// <summary>One tooltip's text as buffer lines: one line per paragraph. A markup-only
        /// line (a layout spacer) would land in the buffer as a silent press, so lines are
        /// kept only when something of them survives the speech filter. A combat item's
        /// glyph-only rank-pip strip is recomposed into spoken Rank/Target lines.</summary>
        public static IEnumerable<string> LinesOf(TooltipUiBhv tooltip) {
            string text = tooltip == null ? null : TextOf(tooltip);
            if (string.IsNullOrWhiteSpace(text)) {
                yield break;
            }
            foreach (var line in text.Split('\n')) {
                var pips = SkillCard.RankPipLines(line);
                if (pips != null) {
                    foreach (var pipLine in pips) {
                        yield return pipLine;
                    }
                } else if (!string.IsNullOrWhiteSpace(Core.Text.TextFilter.Clean(line))) {
                    yield return line;
                }
            }
        }

        public static string TextOf(TooltipUiBhv tooltip) {
            if (tooltip is LocalizedTextTooltipBhv localized) {
                // Resolve the key ourselves: the component's cached Text is only set after its
                // post-start callback ran.
                return GameLoc.TryGet(LocKeyField(localized)) ?? localized.Text;
            }
            if (tooltip is TextTooltipBhv plain) {
                return TextField(plain);
            }
            var type = tooltip.GetType();
            if (!TextFields.TryGetValue(type, out var field)) {
                field = AccessTools.Field(type, "m_text");
                if (field == null || field.FieldType != typeof(string)) {
                    field = null;
                    Plugin.Log.LogWarning("TooltipReader: no m_text on " + type.Name + "; its tooltips will not be read");
                }
                TextFields[type] = field;
            }
            return field == null ? null : (string)field.GetValue(tooltip);
        }
    }
}
