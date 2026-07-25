using Assets.Code.Game;
using Assets.Code.Run;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn's Mastery Trainer (an <c>InnUpgradeSkillsBhv</c> stack entry), named by the
    /// inn header's station title. The skills view: the hero header (name and mastery points
    /// left; Left/Right page the party through the trainer's own arrows), one element per
    /// skill (name, "mastered"/"selected"/"unavailable" state, the full skill card in the
    /// buffer; Enter queues the skill through the trainer's own selection), then the
    /// trainer's remaining buttons (Change Path, Apply, Reset) swept with their own labels.
    /// While the path panel is open it reads instead: the path comparison text, each path
    /// option, and the purchase button. Escape folds the path panel first, then closes.
    /// </summary>
    public sealed class MasteryScreen : GameScreen {
        private static readonly HarmonyLib.AccessTools.FieldRef<InnUpgradeSkillsBhv, GameObject> ResetButtonField =
            HarmonyLib.AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, GameObject>("m_ResetButton");

        private InnUpgradeSkillsBhv _panel;
        private Container _root;
        private int _builtSignature;

        public override string Name => InnStations.Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<InnUpgradeSkillsBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (InnUpgradeSkillsBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                // The trainer's own Escape contract: false = it consumed the press as an
                // internal step (folding the path panel), true = proceed to close.
                if (panel.GoBack()) {
                    panel.CloseSubscreen();
                }
            });
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (InnUpgradeSkillsBhv)target;
            if (Signature(panel) != _builtSignature) {
                _root.Clear();
                Populate(panel);
            }
            return false;
        }

        private void Populate(InnUpgradeSkillsBhv panel) {
            var paths = FindChild(panel.transform, "PathSelectionPanel");
            if (PathViewOpen(paths)) {
                PopulatePaths(paths);
            } else {
                PopulateSkills(panel, paths);
            }
            _builtSignature = Signature(panel);
        }

        // The path panel stays active with its visibility ridden by a CanvasGroup; its
        // interactivity is the reliable open/closed signal.
        private static bool PathViewOpen(Transform paths) {
            var group = paths == null ? null : paths.GetComponent<CanvasGroup>();
            return group != null && group.blocksRaycasts;
        }

        private void PopulateSkills(InnUpgradeSkillsBhv panel, Transform paths) {
            var previous = FindChild(panel.transform, "left_button");
            var next = FindChild(panel.transform, "right_button");
            _root.Add(new MasteryHeroElement(
                () => {
                    var actor = Actors.Get(panel.ActiveActorGuid);
                    return actor == null ? null : actor.ActorName;
                },
                () => (int)Singleton<GameTypeMgr>.Instance.RunValues.GetValue(RunValueType.HERO_UPGRADE_POINTS)
                    - panel.NumSelectedSkills,
                previous == null ? null : previous.GetComponent<Button>(),
                next == null ? null : next.GetComponent<Button>()));

            var skills = new Container(ContainerShape.VerticalList);
            foreach (var button in panel.GetComponentsInChildren<UpgradeSkillButton>(includeInactive: false)) {
                var selectable = button.GetComponent<Selectable>();
                if (selectable != null) {
                    skills.Add(new MasterySkillElement(button, panel, selectable));
                }
            }
            if (!skills.IsEmptyContainer) {
                _root.Add(skills);
            }

            // The trainer's remaining labeled controls (Change Path with its cost, Apply,
            // Reset), excluding what already has an element.
            foreach (var selectable in panel.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable.GetComponent<UpgradeSkillButton>() != null
                    || selectable.gameObject.name == "left_button" || selectable.gameObject.name == "right_button"
                    || selectable is Scrollbar
                    || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null
                    || (paths != null && selectable.transform.IsChildOf(paths))
                    || !UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue;
                }
                var reset = ResetButtonField(panel);
                if (reset != null && selectable.gameObject == reset) {
                    // The visual Reset is a hold gesture; its press handler is the real action.
                    var captured = selectable;
                    _root.Add(new ActionElement(() => UiText.FirstLabel(captured.gameObject),
                        S.RoleButton, panel.OnResetPressed));
                    continue;
                }
                if (selectable.gameObject.name == "PathButton") {
                    // Its caption lives in the tooltip; the visible text is only the cost.
                    var captured = selectable;
                    _root.Add(new SelectableElement(captured, () => {
                        string caption = null;
                        foreach (var line in TooltipReader.Lines(captured.gameObject)) {
                            caption = line;
                            break;
                        }
                        return Core.Text.SpokenLine.Join(caption, UiText.AllText(captured.gameObject));
                    }));
                    continue;
                }
                _root.Add(new SelectableElement(selectable));
            }
        }

        private void PopulatePaths(Transform paths) {
            // The comparison panel carries unbound template labels alongside the live text,
            // so only its named text objects are read.
            var comparison = FindChild(paths, "PathComparisonPanel");
            if (comparison != null) {
                var title = FindChild(comparison, "Title");
                var flavour = FindChild(comparison, "FlavourText");
                var effects = FindChild(comparison, "EffectText");
                _root.Add(new ReadoutElement(() => Core.Text.SpokenLine.Join(
                    title == null ? null : UiText.AllText(title.gameObject),
                    flavour == null ? null : UiText.AllText(flavour.gameObject),
                    effects == null ? null : UiText.AllText(effects.gameObject))));
            }
            foreach (var selectable in paths.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable is Scrollbar || !UiText.HasAnyTextSource(selectable.gameObject)) {
                    continue;
                }
                _root.Add(new SelectableElement(selectable));
            }
        }

        private static int Signature(InnUpgradeSkillsBhv panel) {
            int signature = 17;
            var paths = FindChild(panel.transform, "PathSelectionPanel");
            signature = signature * 31 + (PathViewOpen(paths) ? 1 : 0);
            foreach (var button in panel.GetComponentsInChildren<UpgradeSkillButton>(includeInactive: false)) {
                signature = signature * 31 + button.GetInstanceID();
            }
            foreach (var selectable in panel.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                signature = signature * 31 + selectable.GetInstanceID();
            }
            return signature;
        }

        private static Transform FindChild(Transform root, string name) {
            if (root == null) {
                return null;
            }
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true)) {
                if (child.name == name) {
                    return child;
                }
            }
            return null;
        }
    }
}
