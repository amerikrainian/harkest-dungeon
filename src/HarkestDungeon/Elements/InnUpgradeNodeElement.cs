using System.Collections.Generic;
using Assets.Code.Cost;
using Assets.Code.Data;
using Assets.Code.Inn;
using Assets.Code.UI.Inn;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One node of a kingdom inn's upgrade tree: owned state ahead of the upgrade's own name,
    /// or the game's composed cost line after it, with the upgrade's description in the buffer. Enter
    /// drives the game's gated unlock (the sighted gesture is a hold); an unaffordable or
    /// locked node speaks unavailable instead, matching the game's own refusal.
    /// </summary>
    public sealed class InnUpgradeNodeElement : UIElement {
        private readonly InnUpgradeButtonBhv _node;

        public InnUpgradeNodeElement(InnUpgradeButtonBhv node) {
            _node = node;
        }

        public override bool CanFocus => _node != null && _node.gameObject.activeInHierarchy;

        public override string Label => _node == null ? null : GameLoc.TryGet(_node.InnUpgradeDefinition.m_Id);

        public override string Role => S.RoleButton;

        public override string Status {
            get {
                if (_node == null) {
                    return null;
                }
                return Singleton<InnBhv>.Instance.GetInnInstance().GetHasInnUpgrade(_node.InnUpgradeDefinition)
                    ? S.StatusOwned : null;
            }
        }

        // The cost, then why the node cannot be bought, mirroring what the tree shows: the
        // level-restriction banner over out-of-tier rows, the prerequisite wiring (spoken as
        // the required upgrades' names), and the red cost for an unaffordable one.
        public override string Value {
            get {
                if (_node == null) {
                    return null;
                }
                var definition = _node.InnUpgradeDefinition;
                var inn = Singleton<InnBhv>.Instance.GetInnInstance();
                if (inn.GetHasInnUpgrade(definition)) {
                    return null;
                }
                string cost = CostDescription.GetStoreBuyDescription(definition.CostDefinition, 1f);
                string reason = null;
                int limit = inn.GetUpgradeCategoryLimitLevel(definition.m_InnUpgradeCategory);
                if (limit > 0 && definition.m_InnLevel > limit) {
                    string banner = GameLoc.TryGet("kingdom_inn_upgrade_level_restriction_locked_label");
                    reason = banner == null ? S.StatusUnavailable : string.Format(banner, definition.m_InnLevel);
                } else if (!inn.GetArePurchasePrerequisitesMet(definition)) {
                    reason = S.RequiresUpgrade(PrerequisiteNames(definition));
                } else if (!_node.CanUpgrade) {
                    reason = S.StatusUnavailable; // e.g. another ultimate already owned
                }
                string funds = CostCalculation.CanAffordCost(definition.CostDefinition)
                    ? null : GameLoc.TryGet("insufficient_funds_label") ?? S.StatusUnavailable;
                return SpokenLine.Join(cost, reason, funds);
            }
        }

        private static string PrerequisiteNames(InnUpgradeDefinition definition) {
            var names = new List<string>();
            foreach (var upgrade in definition.PrerequisiteAllInnUpgrades) {
                names.Add(GameLoc.TryGet(upgrade.m_Id));
            }
            foreach (var upgrade in definition.PrerequisiteAnyInnUpgrades) {
                names.Add(GameLoc.TryGet(upgrade.m_Id));
            }
            return SpokenLine.Join(names.ToArray());
        }

        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                if (_node.CanUpgrade
                    && CostCalculation.CanAffordCost(_node.InnUpgradeDefinition.CostDefinition)) {
                    _node.Unlock();
                } else {
                    Core.Speech.SpeechPipeline.Instance?.Speak(S.StatusUnavailable);
                }
            });
        }

        protected override IEnumerable<string> GetDetailLines() {
            if (_node == null) {
                yield break;
            }
            var definition = _node.InnUpgradeDefinition;
            string description = GameLoc.TryGet(definition.m_Id + "_description");
            if (!string.IsNullOrEmpty(description)) {
                yield return description;
            }
            // The ultimate's flavour, which the tree displays beside its node.
            if (definition.m_InnUpgradeType == InnUpgradeType.ULTIMATE) {
                string verbose = GameLoc.TryGet(
                    "inn_upgrade_category_" + definition.m_InnUpgradeCategory.GetName() + "_ult_verbose");
                if (!string.IsNullOrEmpty(verbose)) {
                    yield return verbose;
                }
            }
        }
    }
}
