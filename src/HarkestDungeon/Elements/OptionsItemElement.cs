using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Options;
using DD2A11y.Game;
using HarmonyLib;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A settings row spawned from an <see cref="OptionsValue"/>: the label comes from the
    /// option's own loc key (the row's DataContext binders apply text a frame late, so keys
    /// are the reliable source), the control is the row's Toggle or Slider. The buffer carries
    /// the row's live tooltip binding - the option's description, or, on a locked altar row,
    /// the unlock requirement the game swaps in.
    /// </summary>
    public sealed class OptionsItemElement : SelectableElement {
        private static readonly AccessTools.FieldRef<OptionsItemBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<OptionsItemBhv, DataContextBhv>("m_dataContextBhv");

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

        protected override IEnumerable<string> GetDetailLines() {
            // The binding holds a loc key: the option's tooltip key, or the unlock-requirement
            // key SetLocked swapped in on a locked altar row.
            var context = ContextField(_item);
            string stored = context == null ? null : context.GetStringValue("option_tooltip");
            string tooltip = GameLoc.TryGet(stored) ?? GameLoc.TryGet(_item.OptionValue.m_tooltipLocKey);
            if (!string.IsNullOrEmpty(tooltip)) {
                yield return tooltip;
            }
        }
    }
}
