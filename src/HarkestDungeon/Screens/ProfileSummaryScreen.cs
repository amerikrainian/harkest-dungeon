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
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The profile summary (a modal stack screen the pause menu's profile badge and the title
    /// menu's profile button open), named by the game's own title. The profile's tallies read
    /// first as label-and-value rows in the panel's own order (expeditions, victories, candles,
    /// the altar, item, story and cosmetic percentages), then the achievements under the game's
    /// own "Achievements" header - each row its title, with the description and the progress
    /// count as the value, a hidden one as the game's own placeholder - with the game's group
    /// sub-headers where it inserts them, and the Return button last. Escape is that button's
    /// own click.
    /// </summary>
    public sealed class ProfileSummaryScreen : GameScreen {
        private ProfileSummaryWidgetBhv _widget;
        private Container _root;
        private int _builtSignature;

        public override string Name => GameLoc.TryGet("profile_summary_title_label") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<ProfileSummaryWidgetBhv>(includeInactive: false);
            return _widget;
        }

        // The tallies are data-bound and the achievement rows spawn in the widget's
        // open-completed step, so the entry waits for the screen's own Open state.
        public override bool EntrySettled =>
            _widget != null && _widget.GetComponentInParent<UiScreenBhv>().ScreenState == UiScreenState.Open;

        public override Container BuildRoot(object target) {
            var widget = (ProfileSummaryWidgetBhv)target;
            var confirm = ConfirmButton(widget);
            System.Action back;
            if (confirm != null) {
                back = () => confirm.onClick.Invoke();
            } else {
                back = widget.GetComponentInParent<UiScreenBhv>().TryCloseScreen;
            }
            _root = new RootContainer(ContainerShape.VerticalList, back: back);
            Populate(widget, confirm);
            return _root;
        }

        // The achievement rows arrive after the push; the rebuild lands before the entry
        // announcement (the entry waits for Open), so it needs no announcement of its own.
        public override bool OnUpdate(object target) {
            var widget = (ProfileSummaryWidgetBhv)target;
            if (Signature(widget) != _builtSignature) {
                _root.Clear();
                Populate(widget, ConfirmButton(widget));
            }
            return false;
        }

        private void Populate(ProfileSummaryWidgetBhv widget, Button confirm) {
            var summary = FindChild(widget.transform, "SummaryContainer");
            if (summary != null) {
                foreach (Transform row in summary) {
                    if (!row.gameObject.activeInHierarchy) {
                        continue;
                    }
                    var captured = row;
                    _root.Add(new ReadoutElement(() => OwnText(captured), () => UiText.ChildLabel(captured.gameObject, "Value")));
                }
            }
            var achievements = FindChild(widget.transform, "AchievementsContainer");
            if (achievements != null) {
                var title = FindChild(achievements, "Title");
                if (title != null) {
                    _root.Add(new StaticTextElement(() => OwnText(title)));
                }
                foreach (var row in AchievementRows(achievements)) {
                    var captured = row;
                    var heading = captured.GetComponent<TMP_Text>();
                    if (heading != null) {
                        // A group sub-header the widget instantiates between rows.
                        _root.Add(new StaticTextElement(() => OwnText(captured)));
                    } else {
                        _root.Add(new ReadoutElement(
                            () => UiText.ChildLabel(captured.gameObject, "AchievementTitle"),
                            () => SpokenLine.Join(UiText.ChildLabel(captured.gameObject, "AchievementDescription"),
                                UiText.ChildLabel(captured.gameObject, "ProgressText"))));
                    }
                }
            }
            if (confirm != null) {
                _root.Add(new SelectableElement(confirm));
            }
            _builtSignature = Signature(widget);
        }

        // The rows and sub-headers under the achievements list, in the game's order; the pool's
        // inactive template stays out.
        private static IEnumerable<Transform> AchievementRows(Transform achievements) {
            var list = FindChild(achievements, "Container");
            if (list == null) {
                yield break;
            }
            foreach (Transform row in list) {
                if (row.gameObject.activeInHierarchy) {
                    yield return row;
                }
            }
        }

        private static int Signature(ProfileSummaryWidgetBhv widget) {
            int signature = 17;
            var achievements = FindChild(widget.transform, "AchievementsContainer");
            if (achievements != null) {
                foreach (var row in AchievementRows(achievements)) {
                    signature = signature * 31 + row.GetInstanceID();
                }
            }
            return signature;
        }

        private static Button ConfirmButton(ProfileSummaryWidgetBhv widget) {
            var confirm = FindChild(widget.transform, "ConfirmBtn");
            return confirm == null ? null : confirm.GetComponent<Button>();
        }

        // The text of the object's own label, not its children's (a stat row carries its
        // caption and, beneath it, the value).
        private static string OwnText(Transform row) {
            var tmp = row.GetComponent<TMP_Text>();
            return tmp == null || string.IsNullOrWhiteSpace(tmp.text) ? null : tmp.text;
        }

        private static Transform FindChild(Transform root, string name) => InventoryPanel.FindChild(root, name);
    }
}
