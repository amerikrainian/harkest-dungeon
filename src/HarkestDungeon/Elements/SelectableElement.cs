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
    /// actions), Left/Right adjust sliders in place, and Enter on a dropdown opens its choices
    /// as an option popup. Tooltips in the row scope become buffer lines.
    /// </summary>
    public class SelectableElement : UIElement {
        protected readonly Selectable Selectable;
        private readonly Func<string> _label;
        private readonly GameObject _rowScope;
        private readonly Func<string> _value;

        /// <param name="rowScope">The object whose texts/tooltips describe this control (the row
        /// containing label + control), defaulting to the selectable's own object.</param>
        /// <param name="value">Overrides the value slot (a button whose label alone does not say
        /// what it does - the profile button reads the profile's name, its purpose rides here
        /// from the game's tooltip).</param>
        public SelectableElement(Selectable selectable, Func<string> label = null, GameObject rowScope = null,
                                 Func<string> value = null) {
            Selectable = selectable;
            _label = label;
            _rowScope = rowScope;
            _value = value;
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

        public override string Status {
            get {
                string state = null;
                if (Selectable is Toggle toggle) {
                    state = toggle.isOn ? S.StatusOn : S.StatusOff;
                }
                // A locked control still shows its state (the altar's locked toggles keep their
                // checkmark); both the state and the lock are gameplay-relevant.
                if (Selectable != null && !Selectable.interactable) {
                    return Core.Text.SpokenLine.Join(state, S.StatusUnavailable);
                }
                return state;
            }
        }

        public override string Value {
            get {
                if (_value != null) {
                    return _value();
                }
                if (Selectable is Slider slider) {
                    return S.ValuePercent(Mathf.RoundToInt(slider.normalizedValue * 100f));
                }
                if (Selectable is TMP_Dropdown dropdown) {
                    return DropdownChoice(dropdown);
                }
                return null;
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
                yield break; // Enter opens the option popup via BuildPopup
            }
            yield return new ElementAction(ActionIds.Activate, Submit);
        }

        /// <summary>A dropdown's choices as an option popup: one action per option (committing
        /// fires the game's own onValueChanged), read live from the dropdown. The game's own list
        /// is shown alongside so the screen matches what the popup reads.</summary>
        public override Popup BuildPopup() {
            if (!(Selectable is TMP_Dropdown dropdown) || !dropdown.interactable) {
                return null;
            }
            int count = dropdown.options?.Count ?? 0;
            if (count == 0) {
                return null;
            }
            var list = new Container(ContainerShape.VerticalList, Label);
            for (int i = 0; i < count; i++) {
                int index = i;
                list.Add(new ActionElement(
                    () => dropdown.options[index].text,
                    null,
                    () => dropdown.value = index)); // fires the game's onValueChanged
            }
            dropdown.Show();
            return new Popup(list, () => {
                if (dropdown != null) {
                    dropdown.Hide();
                }
            });
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

        // The uGUI submit path runs everything a controller press would: Button.onClick,
        // Toggle flip, HighlightableButtonBhv's submit action.
        protected void Submit() {
            ExecuteEvents.Execute(Selectable.gameObject, new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string label = Label;
            string value = Value;
            foreach (var line in TooltipReader.Lines(RowScope)) {
                // A tooltip doubling as the label (an icon button) or the value (the profile
                // button's purpose) is already in the focus line.
                if (line != label && line != value) {
                    yield return line;
                }
            }
        }
    }
}
