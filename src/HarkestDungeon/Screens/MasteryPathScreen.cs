using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The mastery trainer's Change Path panel, its own screen above the trainer so opening and
    /// closing announce themselves: named by the panel's own title, one element per path seal
    /// (Enter previews through the trainer's SelectPath), the comparison readout with the
    /// previewed path's live card in its buffer, then the purchase button that commits (the
    /// game closes the panel itself, dropping back to the trainer). Escape closes through the
    /// trainer's own toggle.
    /// </summary>
    public sealed class MasteryPathScreen : GameScreen {
        private static readonly AccessTools.FieldRef<InnUpgradeSkillsBhv, Dictionary<GameObject, string>> PathsField =
            AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, Dictionary<GameObject, string>>("m_pathsAdded");
        private static readonly AccessTools.FieldRef<InnUpgradeSkillsBhv, Button> PurchaseField =
            AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, Button>("m_pathPurchaseButton");
        private static readonly AccessTools.FieldRef<InnUpgradeSkillsBhv, ActorPathComparisonBhv> ComparisonField =
            AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, ActorPathComparisonBhv>("m_pathComparisonBhv");

        private Container _root;
        private int _builtPaths;

        public override string Name => GameLoc.TryGet("inn_path_switching_title") ?? S.ScreenPathSelect;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            var panel = top == null ? null : top.GetComponent<InnUpgradeSkillsBhv>();
            return panel != null && MasteryScreen.PathViewOpen(panel) ? panel : null;
        }

        public override Container BuildRoot(object target) {
            var panel = (InnUpgradeSkillsBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: panel.TogglePathSwitchingPanel);
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (InnUpgradeSkillsBhv)target;
            // The seals are pooled; a repopulate while open swaps them for fresh instances.
            if (PathCount(panel) != _builtPaths) {
                _root.Clear();
                Populate(panel);
            }
            return false;
        }

        private static int PathCount(InnUpgradeSkillsBhv panel) {
            var paths = PathsField(panel);
            return paths == null ? 0 : paths.Count;
        }

        private void Populate(InnUpgradeSkillsBhv panel) {
            _builtPaths = PathCount(panel);
            var paths = PathsField(panel);
            if (paths != null) {
                foreach (var entry in paths) {
                    if (entry.Key != null && entry.Key.activeInHierarchy) {
                        _root.Add(new PathOptionElement(panel, entry.Key));
                    }
                }
            }
            var comparison = ComparisonField(panel);
            if (comparison != null) {
                _root.Add(new ReadoutElement(() => S.PathDetails, detail: () => PathComparison.Lines(comparison)));
            }
            var purchase = PurchaseField(panel);
            if (purchase != null && purchase.gameObject.activeInHierarchy) {
                _root.Add(new SelectableElement(purchase));
            }
        }
    }
}
