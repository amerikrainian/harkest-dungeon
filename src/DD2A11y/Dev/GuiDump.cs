using System.Text;
using Assets.Code.Locale;
using Assets.Code.UI.Tooltips;
using DD2A11y.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Dev {
    /// <summary>Raw dump of the active uGUI hierarchy for the dev loop: paths, control types,
    /// visible text, loc keys, tooltip text. Surfaces structure /nav hides.</summary>
    internal static class GuiDump {
        private const int MaxLines = 8000;

        public static string Dump() {
            var sb = new StringBuilder();
            int lines = 0;
            foreach (var canvas in Object.FindObjectsOfType<Canvas>()) {
                if (!canvas.isRootCanvas) {
                    continue;
                }
                Walk(canvas.transform, 0, sb, ref lines);
                if (lines >= MaxLines) {
                    sb.Append("... truncated\n");
                    break;
                }
            }
            return sb.ToString();
        }

        private static void Walk(Transform node, int depth, StringBuilder sb, ref int lines) {
            if (lines >= MaxLines || !node.gameObject.activeSelf) {
                return;
            }
            lines++;
            sb.Append(' ', depth * 2).Append(node.name);

            var selectable = node.GetComponent<Selectable>();
            if (selectable != null) {
                sb.Append(" <").Append(selectable.GetType().Name);
                if (!selectable.interactable) {
                    sb.Append(" disabled");
                }
                sb.Append('>');
            }
            var tmp = node.GetComponent<TMP_Text>();
            if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text)) {
                sb.Append(" \"").Append(Snip(tmp.text)).Append('"');
            }
            var localize = node.GetComponent<LocalizeTextBhv>();
            if (localize != null && !string.IsNullOrEmpty(localize.locKey)) {
                sb.Append(" loc:").Append(localize.locKey);
            }
            var tooltip = node.GetComponent<TooltipUiBhv>();
            if (tooltip != null) {
                string text = TooltipReader.TextOf(tooltip);
                if (!string.IsNullOrWhiteSpace(text)) {
                    sb.Append(" tip:\"").Append(Snip(text)).Append('"');
                }
            }
            sb.Append('\n');

            for (int i = 0; i < node.childCount; i++) {
                Walk(node.GetChild(i), depth + 1, sb, ref lines);
            }
        }

        private static string Snip(string text) {
            text = text.Replace('\n', ' ').Replace('\r', ' ');
            return text.Length <= 60 ? text : text.Substring(0, 60) + "...";
        }
    }
}
