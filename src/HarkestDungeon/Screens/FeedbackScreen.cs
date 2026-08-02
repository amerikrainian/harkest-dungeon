using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The game's user-report form (the pause menu's Feedback; Unity's user-reporting package,
    /// built from LEGACY uGUI widgets the generic paths misread). Summary and description are
    /// legacy InputFields: Enter starts the field's own edit - typing flows in at device level
    /// while every mod key pauses - keystrokes echo from the field's text diff, and the edit's
    /// end reads the field back. The category is a legacy Dropdown, opened as an option popup
    /// like the TMP ones. The remaining buttons sweep generically (Submit sits unavailable
    /// until the summary validates); Escape is the form's own cancel. The privacy dialog ahead
    /// of the form is an ordinary confirmation dialog.
    /// </summary>
    public sealed class FeedbackScreen : GameScreen {
        private readonly Action<string, bool> _speak;
        private readonly TypingEcho _echo;
        private UserReportingUiBhv _report;
        private Container _root;
        private Container _buttons;
        private int _builtButtons;
        private InputField _edited; // the field the echo followed, for the end-of-edit read-back

        public FeedbackScreen(Action<string, bool> speak) {
            _speak = speak;
            _echo = new TypingEcho(() => EditedField() != null, EditedText, speak);
        }

        public override string Name {
            get {
                string title = UiText.FirstLabel(_report != null ? _report.gameObject : null);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        /// <summary>Whether a report field is capturing keystrokes right now; the input manager
        /// asks this so the mod's keys pause (the package's fields never set the game's own
        /// IsInputtingText).</summary>
        public bool Editing => EditedField() != null;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _report = top == null ? null : top.GetComponent<UserReportingUiBhv>();
            return _report;
        }

        public override Container BuildRoot(object target) {
            var report = (UserReportingUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: report.CancelUserReport);
            _root.Add(new ReportFieldElement(report.SummaryInput));
            _root.Add(new ReportFieldElement(report.DescriptionInput));
            _root.Add(new LegacyDropdownElement(report.CategoryDropdown));
            _buttons = new Container(ContainerShape.VerticalList);
            _root.Add(_buttons);
            RebuildButtons(report);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var report = (UserReportingUiBhv)target;
            var edited = EditedField();
            if (edited != null) {
                _edited = edited;
            }
            if (_echo.Tick()) {
                // The edit ended (Enter, Escape, or the game moved on): read the field back.
                _speak(SpokenLine.Join(ReportFieldElement.Title(_edited), EditedText()), true);
            }
            // The screen enters on its screenshot-capture phase with the form hidden; the
            // buttons arrive when the form shows. The fields' elements read live, so only the
            // swept buttons need the rebuild.
            if (ButtonSignature(report) != _builtButtons) {
                RebuildButtons(report);
            }
            return false;
        }

        private void RebuildButtons(UserReportingUiBhv report) {
            _buttons.Clear();
            foreach (var button in report.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    _buttons.Add(new SelectableElement(button));
                }
            }
            _builtButtons = ButtonSignature(report);
        }

        private static int ButtonSignature(UserReportingUiBhv report) {
            int signature = 17;
            foreach (var button in report.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    signature = signature * 31 + button.GetInstanceID();
                }
            }
            return signature;
        }

        private InputField EditedField() {
            if (_report == null) {
                return null;
            }
            if (_report.SummaryInput != null && _report.SummaryInput.isFocused) {
                return _report.SummaryInput;
            }
            if (_report.DescriptionInput != null && _report.DescriptionInput.isFocused) {
                return _report.DescriptionInput;
            }
            return null;
        }

        private string EditedText() => _edited == null ? "" : _edited.text;

        /// <summary>A legacy InputField row: the game's placeholder is the title (read live even
        /// once typed text hides it), the typed text is the value, Enter hands the keyboard to
        /// the field's own edit (the echo speaks the start).</summary>
        private sealed class ReportFieldElement : UIElement {
            private readonly InputField _field;

            public ReportFieldElement(InputField field) => _field = field;

            public override bool CanFocus => _field != null && _field.gameObject.activeInHierarchy;

            public override string Label => Title(_field);

            public override string Role => S.RoleEdit;

            public override string Value => _field == null || _field.text.Length == 0 ? null : _field.text;

            public static string Title(InputField field) {
                var placeholder = field == null ? null : field.placeholder as Text;
                return placeholder == null ? null : placeholder.text;
            }

            public override IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, () => {
                    EventSystem.current.SetSelectedGameObject(_field.gameObject);
                    _field.ActivateInputField();
                });
            }
        }

        /// <summary>A legacy Dropdown read like the TMP ones: the current choice is the label,
        /// Enter opens the choices as an option popup, committing fires the game's own
        /// onValueChanged.</summary>
        private sealed class LegacyDropdownElement : UIElement {
            private readonly Dropdown _dropdown;

            public LegacyDropdownElement(Dropdown dropdown) => _dropdown = dropdown;

            public override bool CanFocus => _dropdown != null && _dropdown.gameObject.activeInHierarchy;

            public override string Label {
                get {
                    if (_dropdown == null || _dropdown.options == null
                        || _dropdown.value < 0 || _dropdown.value >= _dropdown.options.Count) {
                        return null;
                    }
                    return _dropdown.options[_dropdown.value].text;
                }
            }

            public override string Role => S.RoleDropdown;

            public override Popup BuildPopup() {
                if (_dropdown == null || !_dropdown.interactable) {
                    return null;
                }
                int count = _dropdown.options?.Count ?? 0;
                if (count == 0) {
                    return null;
                }
                var list = new Container(ContainerShape.VerticalList, Label);
                for (int i = 0; i < count; i++) {
                    int index = i;
                    list.Add(new ActionElement(
                        () => _dropdown.options[index].text,
                        null,
                        () => _dropdown.value = index)); // fires the game's own onValueChanged
                }
                _dropdown.Show();
                return new Popup(list, () => {
                    if (_dropdown != null) {
                        _dropdown.Hide();
                    }
                });
            }
        }
    }
}
