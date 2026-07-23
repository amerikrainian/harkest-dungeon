using System.Collections.Generic;
using Assets.Code.UI.HeroSelect;
using Assets.Code.UI.Tooltips;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A hero portrait slot at the crossroads (party or roster). The hero's name and detail live
    /// in the slot's tooltip; the first tooltip line names the focus, the rest go to the buffer.
    /// Activation clicks the slot's own button (the game's select/assign logic).
    /// </summary>
    public sealed class HeroSlotElement : SelectableElement {
        private static readonly AccessTools.FieldRef<HeroSelectActorUIBhv, TextTooltipBhv> TooltipField =
            AccessTools.FieldRefAccess<HeroSelectActorUIBhv, TextTooltipBhv>("m_tooltipBhv");

        private readonly HeroSelectActorUIBhv _slot;

        public HeroSlotElement(HeroSelectActorUIBhv slot, Button button)
            : base(button, null, slot.gameObject) {
            _slot = slot;
        }

        public override string Label {
            get {
                if (!_slot.IsOccupied && !_slot.IsLocked()) {
                    return S.CrossroadsEmptySlot;
                }
                string tooltip = TooltipText();
                if (!string.IsNullOrWhiteSpace(tooltip)) {
                    // First tooltip line = the hero's name; the rest is buffer detail.
                    foreach (var line in tooltip.Split('\n')) {
                        if (!string.IsNullOrWhiteSpace(line)) {
                            return line;
                        }
                    }
                }
                return UiText.FirstLabel(_slot.gameObject);
            }
        }

        public override string Value {
            get {
                if (_slot.IsLocked()) {
                    return S.StatusUnavailable;
                }
                return base.Value;
            }
        }

        private string TooltipText() {
            var tooltip = TooltipField(_slot);
            return tooltip == null ? null : TooltipReader.TextOf(tooltip);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string tooltip = TooltipText();
            if (tooltip == null) {
                yield break;
            }
            bool first = true;
            foreach (var line in tooltip.Split('\n')) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                if (first) {
                    first = false; // the name line already led the focus text
                    continue;
                }
                yield return line;
            }
        }
    }
}
