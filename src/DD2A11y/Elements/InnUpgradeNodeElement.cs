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
    /// One node of a kingdom inn's upgrade tree: the upgrade's own name, then owned state or
    /// the game's composed cost line, with the upgrade's description in the buffer. Enter
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

        public override string Value {
            get {
                if (_node == null) {
                    return null;
                }
                var definition = _node.InnUpgradeDefinition;
                if (Singleton<InnBhv>.Instance.GetInnInstance().GetHasInnUpgrade(definition)) {
                    return S.StatusOwned;
                }
                string cost = CostDescription.GetStoreBuyDescription(definition.CostDefinition, 1f);
                return _node.CanUpgrade ? cost : SpokenLine.Join(cost, S.StatusUnavailable);
            }
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

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string description = _node == null ? null
                : GameLoc.TryGet(_node.InnUpgradeDefinition.m_Id + "_description");
            if (!string.IsNullOrEmpty(description)) {
                yield return description;
            }
        }
    }
}
