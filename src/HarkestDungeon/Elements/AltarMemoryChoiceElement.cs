using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Item;
using Assets.Code.Profile;
using Assets.Code.UI.Items;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One memory offer in the Timeless Wood's selection list: the memory item's name with
    /// its candle cost from the game's own binding (a reroll's replacement offers are free -
    /// the reroll itself was the purchase), the item tooltip in the buffer. Enter is the
    /// game's own select-and-buy; it is pre-gated on the cost, because the game's own
    /// failure path closes the whole list instead of refusing.
    /// </summary>
    public sealed class AltarMemoryChoiceElement : UIElement {
        private static readonly AccessTools.FieldRef<AltarSelectMemoryBhv, IReadOnlyItemInstance> MemoryField =
            AccessTools.FieldRefAccess<AltarSelectMemoryBhv, IReadOnlyItemInstance>("m_memoryInstance");
        private static readonly AccessTools.FieldRef<AltarSelectMemoryBhv, bool> RerollField =
            AccessTools.FieldRefAccess<AltarSelectMemoryBhv, bool>("m_isReroll");
        private static readonly AccessTools.FieldRef<AltarSelectMemoryBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<AltarSelectMemoryBhv, DataContextBhv>("m_dataContextBhv");

        private readonly AltarSelectMemoryBhv _choice;

        public AltarMemoryChoiceElement(AltarSelectMemoryBhv choice) {
            _choice = choice;
        }

        public override bool CanFocus => _choice != null && _choice.gameObject.activeInHierarchy;

        public override string Label {
            get {
                var memory = MemoryField(_choice);
                return memory == null ? null : ItemDescription.GetTitle(memory.GetItemDefinition());
            }
        }

        public override string Role => S.RoleButton;

        private int Cost {
            get {
                int cost;
                return int.TryParse(ContextField(_choice).GetStringValue("cost"), out cost) ? cost : 0;
            }
        }

        public override string Value => RerollField(_choice) ? null : S.AltarCandleCost(Cost);

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                if (!RerollField(_choice) && !SingletonMonoBehaviour<ProfileBhv>.Instance
                        .GetCurrentProfile().CanAffordCandleCost(Cost)) {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                    return;
                }
                _choice.SelectMemory();
            });
        }

        protected override IEnumerable<string> GetDetailLines() => TooltipReader.Lines(_choice.gameObject);
    }
}
