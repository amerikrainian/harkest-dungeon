using System;
using Assets.Code.Data;
using Assets.Code.UI.Options;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The settings screen's key-bindings panel (`InputBindingsWidgetBhv`), opened by the
    /// controls tab's Bindings button and named by that button's own caption. One row per
    /// rebindable command, labeled with the command name: Up/Down walk commands, Left/Right its
    /// two key slots, with the game's action-map headers (General, Driving, Combat) in the flow;
    /// the panel's Close and Default Bindings buttons close the list. While the game's
    /// interactive rebind is listening, every mod key pauses (the pressed key must become the
    /// binding, not a navigation step) and the end of the listen reads the slot's outcome - the
    /// new key, or the kept one after Escape. Escape otherwise closes the panel through the
    /// game's own toggle; the settings screen re-announces.
    /// </summary>
    public sealed class KeyBindingsScreen : GameScreen {
        private static readonly AccessTools.FieldRef<InputBindingsWidgetBhv, GameObject> ContainerField =
            AccessTools.FieldRefAccess<InputBindingsWidgetBhv, GameObject>("m_keybindingContainer");
        private static readonly AccessTools.FieldRef<InputBindingsWidgetBhv, Transform> ContentField =
            AccessTools.FieldRefAccess<InputBindingsWidgetBhv, Transform>("m_contentContainer");

        private readonly TraditionalNavigator _navigator;
        private readonly Action<string, bool> _speak;
        private InputBindingsWidgetBhv _widget;
        private Container _root;
        private bool _wasBinding;

        public KeyBindingsScreen(TraditionalNavigator navigator, Action<string, bool> speak) {
            _navigator = navigator;
            _speak = speak;
        }

        public override string Name =>
            GameLoc.TryGet("options_menu_controls_input_bindings_label") ?? S.ScreenKeyBindings;

        /// <summary>Whether the game's interactive rebind is listening for a key. The input
        /// manager pauses all mod keys on it, so the press lands in the binding alone.</summary>
        public bool RebindActive =>
            _widget != null && _widget.gameObject.activeInHierarchy && _widget.IsBinding();

        public override object ResolveTarget() {
            var top = StackTop.Object();
            var options = top == null ? null : top.GetComponent<OptionsMenuUiBhv>();
            if (options == null) {
                return null;
            }
            if (_widget == null) {
                _widget = options.GetComponentInChildren<InputBindingsWidgetBhv>(includeInactive: true);
            }
            if (_widget == null) {
                return null;
            }
            var container = ContainerField(_widget);
            return container != null && container.activeInHierarchy ? _widget : null;
        }

        public override Container BuildRoot(object target) {
            _widget = (InputBindingsWidgetBhv)target;
            _wasBinding = false;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => _widget.ToggleKeybindingContainer());

            var content = ContentField(_widget);
            foreach (Transform child in content) {
                if (!child.gameObject.activeInHierarchy) {
                    continue;
                }
                var row = child.GetComponent<RebindInputActionBhv>();
                if (row != null) {
                    var rowContainer = new Container(ContainerShape.HorizontalList,
                        KeybindSlotElement.CommandLabel(row));
                    rowContainer.Add(new KeybindSlotElement(row, 0, _speak));
                    rowContainer.Add(new KeybindSlotElement(row, 1, _speak));
                    _root.Add(rowContainer);
                    continue;
                }
                // The action-map section headers (General, Driving, Combat) carry a data
                // context; the column-header row does not and stays out of the flow. The
                // header text is read from the context - the pooled row's TMP label still
                // shows its placeholder on the entry announcement.
                var headerContext = child.GetComponent<DataContextBhv>();
                if (headerContext != null) {
                    _root.Add(new StaticTextElement(() => headerContext.GetStringValue("header_label")));
                }
            }

            foreach (var button in ContainerField(_widget).GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.transform.IsChildOf(content) || !UiText.HasAnyTextSource(button.gameObject)) {
                    continue;
                }
                _root.Add(new SelectableElement(button));
            }
            return _root;
        }

        public override bool OnUpdate(object target) {
            bool binding = _widget.IsBinding();
            if (binding != _wasBinding) {
                _wasBinding = binding;
                // The listen just ended: read the slot's outcome from the refreshed model - the
                // new key on a completed rebind, the kept one after Escape.
                if (!binding && _navigator.Current is KeybindSlotElement slot) {
                    _speak(slot.GetValueText(), true);
                }
            }
            return false;
        }

        public override void OnLeave() {
            _wasBinding = false;
        }
    }
}
