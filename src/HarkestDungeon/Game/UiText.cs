using Assets.Code.Locale;
using TMPro;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// Label extraction from live uGUI widgets. Preference order: a LocalizeTextBhv's loc key
    /// resolved through the game's localization (stable across skins and animations), then the
    /// first non-empty ACTIVE TMP text, then legacy Text. Inactive TMP children hold placeholder
    /// text ("Tooltip Text"), so sweeps are active-only.
    /// </summary>
    public static class UiText {
        /// <summary>The best label for a widget rooted at <paramref name="root"/>, or null.</summary>
        public static string FirstLabel(GameObject root) {
            if (root == null) {
                return null;
            }

            var localized = root.GetComponentInChildren<LocalizeTextBhv>(includeInactive: false);
            if (localized != null) {
                string viaKey = GameLoc.TryGet(localized.locKey);
                if (!string.IsNullOrEmpty(viaKey)) {
                    return viaKey;
                }
            }

            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(includeInactive: false)) {
                if (!string.IsNullOrWhiteSpace(tmp.text)) {
                    return tmp.text;
                }
            }

            foreach (var legacy in root.GetComponentsInChildren<UnityEngine.UI.Text>(includeInactive: false)) {
                if (!string.IsNullOrWhiteSpace(legacy.text)) {
                    return legacy.text;
                }
            }

            // Icon-only widgets (the main menu's exit/patch-notes/cinematics buttons) carry their
            // name solely in their tooltip - its first line is the label of last resort.
            foreach (var line in TooltipReader.Lines(root)) {
                return line;
            }

            return null;
        }

        /// <summary>Whether a widget can produce a label right now: any ACTIVE text component,
        /// loc key, or tooltip beneath it (the text itself may still be pending - captions arrive
        /// a beat after the component). Inactive text children are hover-only decorations and
        /// placeholders ("Player Username" on the pause menu's profile badge), so a selectable
        /// with only those is skipped; a screen's count-rebuild picks it up if they ever
        /// activate.</summary>
        public static bool HasAnyTextSource(GameObject root) {
            if (root == null) {
                return false;
            }
            return root.GetComponentInChildren<TMP_Text>(includeInactive: false) != null
                || root.GetComponentInChildren<UnityEngine.UI.Text>(includeInactive: false) != null
                || root.GetComponentInChildren<LocalizeTextBhv>(includeInactive: false) != null
                || root.GetComponentInChildren<Assets.Code.UI.Tooltips.TooltipUiBhv>(includeInactive: false) != null;
        }

        /// <summary>The label under a named active child of <paramref name="root"/> (an altar
        /// sub-screen's "exit_anchor" carries the panel title), or null when absent.</summary>
        public static string ChildLabel(GameObject root, string childName) {
            if (root == null) {
                return null;
            }
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: false)) {
                if (child.name == childName) {
                    return FirstLabel(child.gameObject);
                }
            }
            return null;
        }

        /// <summary>All non-empty active TMP text under a root, joined as one line (a disclaimer,
        /// a modal body spread over several labels).</summary>
        public static string AllText(GameObject root) {
            if (root == null) {
                return null;
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(includeInactive: false)) {
                if (!string.IsNullOrWhiteSpace(tmp.text)) {
                    parts.Add(tmp.text);
                }
            }
            return parts.Count == 0 ? null : string.Join(". ", parts);
        }

        /// <summary>Every non-empty active TMP text under a root, ONE LINE PER LABEL - the
        /// buffer's form of <see cref="AllText"/>, for a panel whose parts are reviewed
        /// separately (a path's name, flavour, and effect lines).</summary>
        public static System.Collections.Generic.IEnumerable<string> AllTextLines(GameObject root) {
            if (root == null) {
                yield break;
            }
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(includeInactive: false)) {
                if (string.IsNullOrWhiteSpace(tmp.text)) {
                    continue;
                }
                foreach (var line in tmp.text.Split('\n')) {
                    string clean = Core.Text.TextFilter.Clean(line);
                    if (clean.Length > 0) {
                        yield return clean;
                    }
                }
            }
        }
    }
}
