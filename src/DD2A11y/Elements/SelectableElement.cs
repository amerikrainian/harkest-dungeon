using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A generic wrapper over a live uGUI <see cref="Selectable"/>: role and value follow the
    /// concrete type (button, toggle, slider, dropdown), activation goes through the uGUI submit
    /// handler so the game's own logic runs (Button.onClick, HighlightableButtonBhv submit
    /// actions), and Left/Right adjust sliders and dropdowns in place. Tooltips in the row scope
    /// become buffer lines.
    /// </summary>
    public class SelectableElement : UIElement {
        protected readonly Selectable Selectable;
        private readonly Func<string> _label;
        private readonly GameObject _rowScope;

        /// <param name="rowScope">The object whose texts/tooltips describe this control (the row
        /// containing label + control), defaulting to the selectable's own object.</param>
        public SelectableElement(Selectable selectable, Func<string> label = null, GameObject rowScope = null) {
            Selectable = selectable;
            _label = label;
            _rowScope = rowScope;
        }

        protected GameObject RowScope => _rowScope != null ? _rowScope : Selectable != null ? Selectable.gameObject : null;

        public override bool CanFocus => Selectable != null && Selectable.gameObject.activeInHierarchy;

        public override string Label => _label != null ? _label() : UiText.FirstLabel(RowScope);

        public override string Role {
            get {
                if (Selectable is Toggle) {
                    return S.RoleToggle;
                }
                if (Selectable is Slider) {
                    return S.RoleSlider;
                }
                if (Selectable is TMP_Dropdown) {
                    return S.RoleDropdown;
                }
                return S.RoleButton;
            }
        }

        public override string Value {
            get {
                string state = null;
                if (Selectable is Toggle toggle) {
                    state = toggle.isOn ? S.StatusOn : S.StatusOff;
                } else if (Selectable is Slider slider) {
                    state = S.ValuePercent(Mathf.RoundToInt(slider.normalizedValue * 100f));
                } else if (Selectable is TMP_Dropdown dropdown) {
                    state = DropdownChoice(dropdown);
                }
                // A locked control still shows its state (the altar's locked toggles keep their
                // checkmark); both the state and the lock are gameplay-relevant.
                if (Selectable != null && !Selectable.interactable) {
                    return Core.Text.SpokenLine.Join(state, S.StatusUnavailable);
                }
                return state;
            }
        }

        private static string DropdownChoice(TMP_Dropdown dropdown) {
            if (dropdown.options == null || dropdown.value < 0 || dropdown.value >= dropdown.options.Count) {
                return null;
            }
            return dropdown.options[dropdown.value].text;
        }

        public override bool ReannounceOnActivate => Selectable is Toggle;

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.interactable) {
                yield break;
            }
            if (Selectable is Slider) {
                yield return new ElementAction(ActionIds.Increase, () => AdjustSlider(+1));
                yield return new ElementAction(ActionIds.Decrease, () => AdjustSlider(-1));
                yield break;
            }
            if (Selectable is TMP_Dropdown) {
                yield return new ElementAction(ActionIds.Increase, () => AdjustDropdown(+1));
                yield return new ElementAction(ActionIds.Decrease, () => AdjustDropdown(-1));
                yield return new ElementAction(ActionIds.Activate, Submit);
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, Submit);
        }

        // A dropdown's ends are not a magnitude: on a clamped adjust, re-read the choice.
        public override string GetAdjustText(string actionId, bool changed) {
            if (!changed && Selectable is TMP_Dropdown) {
                return GetValueText();
            }
            return base.GetAdjustText(actionId, changed);
        }

        private void AdjustSlider(int direction) {
            var slider = (Slider)Selectable;
            // 5% of the visual range per press; wholeNumbers sliders step by 1.
            if (slider.wholeNumbers) {
                slider.value = Mathf.Clamp(slider.value + direction, slider.minValue, slider.maxValue);
            } else {
                slider.normalizedValue = Mathf.Clamp01(slider.normalizedValue + direction * 0.05f);
            }
        }

        private void AdjustDropdown(int direction) {
            var dropdown = (TMP_Dropdown)Selectable;
            int count = dropdown.options?.Count ?? 0;
            if (count == 0) {
                return;
            }
            int next = Mathf.Clamp(dropdown.value + direction, 0, count - 1);
            if (next != dropdown.value) {
                dropdown.value = next; // fires the game's onValueChanged
            }
        }

        // The uGUI submit path runs everything a controller press would: Button.onClick,
        // Toggle flip, HighlightableButtonBhv's submit action.
        protected void Submit() {
            ExecuteEvents.Execute(Selectable.gameObject, new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string label = Label;
            foreach (var line in TooltipReader.Lines(RowScope)) {
                if (line != label) { // an icon button's tooltip doubled as its label
                    yield return line;
                }
            }
        }
    }
}
