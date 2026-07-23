using System.Collections.Generic;
using Assets.Code.UI.Options;
using DD2A11y.Game;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A settings row spawned from an <see cref="OptionsValue"/>: the label and tooltip come from
    /// the option's own loc keys (the row's DataContext binders apply text a frame late, so keys
    /// are the reliable source), the control is the row's Toggle or Slider.
    /// </summary>
    public sealed class OptionsItemElement : SelectableElement {
        private readonly OptionsItemBhv _item;

        private OptionsItemElement(OptionsItemBhv item, Selectable control)
            : base(control, () => GameLoc.TryGet(item.OptionValue.m_locKey), item.gameObject) {
            _item = item;
        }

        /// <summary>Null for a row that is not a live option (a pool template with no OptionValue,
        /// a row missing its control).</summary>
        public static OptionsItemElement TryCreate(OptionsItemBhv item) {
            if (item == null || item.OptionValue == null) {
                return null;
            }
            Selectable control = item.OptionValue.m_optionType == OptionsValue.OptionType.TOGGLE
                ? item.GetComponentInChildren<Toggle>(includeInactive: false)
                : (Selectable)item.GetComponentInChildren<Slider>(includeInactive: false);
            if (control == null) {
                return null;
            }
            return new OptionsItemElement(item, control);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string tooltip = GameLoc.TryGet(_item.OptionValue.m_tooltipLocKey);
            if (!string.IsNullOrEmpty(tooltip)) {
                yield return tooltip;
            }
        }
    }
}
