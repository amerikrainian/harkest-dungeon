using System.Collections.Generic;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// The credits (a Pause-layer stack screen from the settings menu): a scrolling column of
    /// headings and name-and-title pairs the floor could not read at all, since nothing in it
    /// is selectable. Each heading reads as a text row, and each names column pairs with its
    /// titles column line by line ("Wayne June, Narrator"), in the game's own order. Escape
    /// closes through the screen's own teardown (the sighted gesture is a held Escape).
    /// </summary>
    public sealed class CreditsScreen : GameScreen {
        private CreditsScreenWidgetBhv _widget;

        public override string Name {
            get {
                string title = _widget == null ? null : UiText.FirstLabel(_widget.gameObject);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<CreditsScreenWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (CreditsScreenWidgetBhv)target;
            var screen = widget.GetComponentInParent<UiScreenBhv>();
            var root = new RootContainer(ContainerShape.VerticalList, back: screen.TryCloseScreen);
            var layout = InventoryPanel.FindChild(widget.transform, "Layout");
            if (layout == null) {
                Plugin.Log.LogWarning("CreditsScreen: no 'Layout' under the credits; nothing to read");
                return root;
            }
            foreach (Transform group in layout) {
                if (!group.gameObject.activeInHierarchy) {
                    continue;
                }
                var captured = group;
                var names = ColumnOf(group, "CreditsText");
                var titles = ColumnOf(group, "CreditsJobTitles");
                if (names != null) {
                    for (int i = 0; i < names.Count; i++) {
                        string line = titles != null && i < titles.Count && titles.Count == names.Count
                            ? SpokenLine.Join(names[i], titles[i]) : names[i];
                        string capturedLine = line;
                        root.Add(new StaticTextElement(() => capturedLine));
                    }
                    if (titles != null && titles.Count != names.Count) {
                        // Columns that do not line up read whole, titles after names.
                        foreach (var title in titles) {
                            string capturedTitle = title;
                            root.Add(new StaticTextElement(() => capturedTitle));
                        }
                    }
                } else {
                    root.Add(new StaticTextElement(() => captured == null ? null : UiText.AllText(captured.gameObject)));
                }
            }
            return root;
        }

        // A column's non-empty lines; null when the group carries no such column.
        private static List<string> ColumnOf(Transform group, string name) {
            var column = InventoryPanel.FindChild(group, name);
            var tmp = column == null ? null : column.GetComponent<TMP_Text>();
            if (tmp == null) {
                return null;
            }
            var lines = new List<string>();
            foreach (var line in tmp.text.Split('\n')) {
                string clean = TextFilter.Clean(line);
                if (clean.Length > 0) {
                    lines.Add(clean);
                }
            }
            return lines;
        }
    }
}
