using Assets.Code.Game;
using Assets.Code.Run;
using Assets.Code.UI;
using Assets.Code.UI.Inn;
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
    /// inn header's station title. The hero header (name and mastery points left; Left/Right
    /// page the party through the trainer's own arrows), one element per skill (name,
    /// "mastered"/"selected"/"unavailable" state, the full skill card in the buffer; Enter
    /// queues the skill through the trainer's own selection), then the trainer's remaining
    /// buttons (Change Path, Apply, Reset) swept with their own labels. The Change Path
    /// panel reads as its own screen (<see cref="MasteryPathScreen"/>). A Kingdoms trainer
    /// adds the Skills / Hero Upgrades tabs; the upgrade tab's track rows read as
    /// <see cref="MasteryUnlockElement"/>s, and a purchase (which respawns them) lands back
    /// on the same row.
    /// </summary>
    public sealed class MasteryScreen : GameScreen {
        private static readonly HarmonyLib.AccessTools.FieldRef<InnUpgradeSkillsBhv, GameObject> ResetButtonField =
            HarmonyLib.AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, GameObject>("m_ResetButton");
        // The hero the party browser stands on, kept current by the trainer's own
        // party-browsed listener on either tab. Its public ActiveActorGuid is the skills
        // widget's, which drops its listener while the Hero Upgrades tab hides the widget.
        private static readonly HarmonyLib.AccessTools.FieldRef<InnUpgradeSkillsBhv, uint> BrowsedActorField =
            HarmonyLib.AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, uint>("m_activeActorGuid");

        private InnUpgradeSkillsBhv _panel;
        private Container _root;
        private int _builtSignature;

        // The game's own station name, from the panel's loc key; the inn header's title binds
        // to it a frame after the panel tops the stack.
        public override string Name => _panel != null ? _panel.GetScreenName() : S.ScreenGeneric;

        // The panel tops the stack a frame before its push step (OnScreenPushed, which moves
        // the state to Opening) browses the party to its first hero and spawns the skill rows;
        // read before that, the hero header has no name and the rows are absent.
        public override bool EntrySettled =>
            _panel.ScreenState == UiScreenState.Opening || _panel.ScreenState == UiScreenState.Open;

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
                var focused = FocusedLeaf(_root);
                int unlockIndex = UnlockIndex(focused);
                var control = (focused as SelectableElement)?.Selectable;
                _root.Clear();
                Populate(panel);
                if (unlockIndex >= 0) {
                    SeedUnlock(unlockIndex);
                } else if (control != null) {
                    SeedControl(_root, control);
                }
            }
            return false;
        }

        // The element the cursor stands on: the remembered child at every level down to the
        // leaf (a skill sits one list below the root).
        private static UIElement FocusedLeaf(Container root) {
            UIElement node = root;
            while (node is Container container && container.FocusedChild != null) {
                node = container.FocusedChild;
            }
            return node;
        }

        // A rebuild lands back where the player was: a persistent control (a skill, a tab, the
        // seal) by identity at any depth, so a pick that shows the Reset button leaves the focus
        // on the picked skill and pressing Hero Upgrades leaves it on that tab.
        private static bool SeedControl(Container container, Selectable control) {
            foreach (var child in container.Children) {
                bool found = (child is SelectableElement element && element.Selectable == control)
                    || (child is Container nested && SeedControl(nested, control));
                if (found) {
                    container.SetFocusedChild(child);
                    return true;
                }
            }
            return false;
        }

        // The upgrade track's rows are recycled and respawned on every purchase; the row at
        // the same position is the one the player pressed.
        private int UnlockIndex(UIElement element) {
            int index = 0;
            foreach (var child in _root.Children) {
                if (child is MasteryUnlockElement) {
                    if (child == element) {
                        return index;
                    }
                    index++;
                }
            }
            return -1;
        }

        private void SeedUnlock(int index) {
            if (index < 0) {
                return;
            }
            int seen = 0;
            foreach (var child in _root.Children) {
                if (child is MasteryUnlockElement) {
                    if (seen == index) {
                        _root.SetFocusedChild(child);
                        return;
                    }
                    seen++;
                }
            }
        }

        private void Populate(InnUpgradeSkillsBhv panel) {
            PopulateSkills(panel, FindChild(panel.transform, "PathSelectionPanel"));
            _builtSignature = Signature(panel);
        }

        // The path panel stays active with its visibility ridden by a CanvasGroup; its
        // interactivity is the reliable open/closed signal.
        internal static bool PathViewOpen(InnUpgradeSkillsBhv panel) {
            var paths = FindChild(panel.transform, "PathSelectionPanel");
            var group = paths == null ? null : paths.GetComponent<CanvasGroup>();
            return group != null && group.blocksRaycasts;
        }

        private void PopulateSkills(InnUpgradeSkillsBhv panel, Transform paths) {
            var previous = FindChild(panel.transform, "left_button");
            var next = FindChild(panel.transform, "right_button");
            _root.Add(new MasteryHeroElement(
                () => {
                    var actor = Actors.Get(BrowsedActorField(panel));
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
                var unlock = selectable.GetComponentInParent<InnActorUpgradeUnlockBhv>();
                if (unlock != null) {
                    _root.Add(new MasteryUnlockElement(unlock, panel, selectable));
                    continue;
                }
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

        private static int Signature(InnUpgradeSkillsBhv panel) {
            int signature = 17;
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
