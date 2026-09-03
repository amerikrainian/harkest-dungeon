using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The game's name-input dialog (<c>NameInputDialogBhv</c>, a modal: the pet rename at a
    /// Kingdoms inn). The game opens it typing - its field is active and taking keys, so the
    /// mod's own keys stand down - and the screen echoes each keystroke the way the crossroads
    /// rename does, reading the accepted text when Return ends the edit. Layout: the field
    /// (the dialog's title as its label, the text as its value; Enter starts typing again),
    /// then the confirm button, which commits the text through the dialog's own accept, and
    /// the decline button where the caller offers one. Escape declines the same way.
    /// </summary>
    public sealed class NameInputScreen : GameScreen {
        private static readonly AccessTools.FieldRef<NameInputDialogBhv, TMP_InputField> FieldRef =
            AccessTools.FieldRefAccess<NameInputDialogBhv, TMP_InputField>("m_InputField");
        private static readonly AccessTools.FieldRef<NameInputDialogBhv, GameObject> AcceptField =
            AccessTools.FieldRefAccess<NameInputDialogBhv, GameObject>("m_AcceptBtn");
        private static readonly AccessTools.FieldRef<NameInputDialogBhv, GameObject> DeclineField =
            AccessTools.FieldRefAccess<NameInputDialogBhv, GameObject>("m_DeclineBtn");

        private readonly Action<string, bool> _speak;
        private readonly TypingEcho _echo;
        private NameInputDialogBhv _dialog;

        public NameInputScreen(Action<string, bool> speak) {
            _speak = speak;
            _echo = new TypingEcho(() => _dialog != null && _dialog.IsInputtingText, FieldText, speak);
        }

        public override string Name => Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Raw();
            _dialog = top == null ? null : top.GetComponentInChildren<NameInputDialogBhv>(includeInactive: false);
            return _dialog;
        }

        public override Container BuildRoot(object target) {
            var dialog = (NameInputDialogBhv)target;
            var decline = DeclineField(dialog);
            bool hasDecline = decline != null && decline.activeSelf;
            var root = new RootContainer(ContainerShape.VerticalList,
                back: hasDecline ? dialog.OnDeclinePressed : (Action)null);
            root.Add(new FieldElement(this, dialog));
            var accept = AcceptField(dialog);
            var acceptButton = accept == null ? null : accept.GetComponent<Button>();
            if (acceptButton != null) {
                root.Add(new SelectableElement(acceptButton));
            }
            var declineButton = !hasDecline ? null : decline.GetComponent<Button>();
            if (declineButton != null) {
                root.Add(new SelectableElement(declineButton));
            }
            return root;
        }

        public override bool OnUpdate(object target) {
            if (_echo.Tick()) {
                // Return ended the edit: read the text the dialog will hand to its caller.
                _speak(SpokenLine.Join(Title(), FieldText()), true);
            }
            return false;
        }

        private string Title() {
            var context = _dialog == null ? null : _dialog.GetComponent<DataContextBhv>();
            return context == null ? null : context.GetStringValue("confirmation_title");
        }

        private string FieldText() {
            var field = _dialog == null ? null : FieldRef(_dialog);
            return field == null ? "" : field.text;
        }

        /// <summary>The dialog's text field: title as label, text as value; Enter (re)starts
        /// the game's own edit, which takes the keyboard until Return.</summary>
        private sealed class FieldElement : UIElement {
            private readonly NameInputScreen _screen;
            private readonly NameInputDialogBhv _dialog;

            public FieldElement(NameInputScreen screen, NameInputDialogBhv dialog) {
                _screen = screen;
                _dialog = dialog;
            }

            public override string Label => _screen.Title();

            public override string Role => S.RoleEdit;

            public override string Value => _screen.FieldText();

            public override IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, () => {
                    var field = FieldRef(_dialog);
                    if (field == null) {
                        return;
                    }
                    EventSystem.current.SetSelectedGameObject(field.gameObject);
                    field.ActivateInputField();
                    _dialog.OnEdittingName();
                });
            }
        }
    }
}
