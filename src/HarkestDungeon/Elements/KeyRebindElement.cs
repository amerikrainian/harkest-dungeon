using System.Collections.Generic;
using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One mod-keys row: the command's name and its current key or keys. A command carries a
    /// LIST of bindings; Enter opens the row's menu - add a key, or replace or delete one of
    /// the current keys (add and replace listen for the next non-modifier press, chord-aware;
    /// Escape keeps things as they are). A captured key another command holds is refused by
    /// name (delete it there first), so no command is ever stripped behind the player's back.
    /// Shift+Enter (the discard action) restores the command's authored defaults. The buffer
    /// carries the default.
    /// </summary>
    public sealed class KeyRebindElement : UIElement {
        private readonly InputAction _action;
        private readonly ModKeymap _keymap;
        private readonly ModRebind _rebind;
        private readonly System.Action<string, bool> _speak;
        // Set while the listen this row started is live; the row's focus text becomes the
        // prompt, so the popup close's restored-row re-read IS the prompt (no speech race).
        private bool _listening;
        private string _prompt;

        public KeyRebindElement(InputAction action, ModKeymap keymap, ModRebind rebind,
                                System.Action<string, bool> speak) {
            _action = action;
            _keymap = keymap;
            _rebind = rebind;
            _speak = speak;
        }

        public override string Label => _action.Label;

        public override string Value => Display(_action.Bindings);

        public override string GetFocusText()
            => _listening && _rebind.Active ? _prompt : base.GetFocusText();

        public override Popup BuildPopup() {
            var root = new Container(ContainerShape.VerticalList, _action.Label);
            root.Add(new ActionElement(() => S.KeyAddBinding, null,
                () => StartListen(ListenMode.Keyboard, replacing: null)));
            if (UnityEngine.InputSystem.Gamepad.current != null) {
                root.Add(new ActionElement(() => S.KeyAddPadBinding, null,
                    () => StartListen(ListenMode.Pad, replacing: null)));
            }
            foreach (var binding in _action.Bindings) {
                var existing = binding;
                var mode = existing is PadBinding ? ListenMode.Pad : ListenMode.Keyboard;
                root.Add(new ActionElement(() => S.KeyReplaceBinding(existing.DisplayName), null,
                    () => StartListen(mode, existing)));
                root.Add(new ActionElement(() => S.KeyDeleteBinding(existing.DisplayName), null,
                    () => _keymap.Remove(_action, existing)));
            }
            return new Popup(root);
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction("discard", ResetToDefault);
        }

        protected override IEnumerable<string> GetDetailLines() {
            yield return S.KeyDefault(Display(_keymap.DefaultsOf(_action)));
        }

        // Add a captured key or pad input, or swap it in for <paramref name="replacing"/>. A
        // combo the command already carries reads the row back unchanged (capturing the
        // replaced one included).
        private void StartListen(ListenMode mode, Core.Input.InputBinding replacing) {
            _listening = true;
            _prompt = mode == ListenMode.Pad ? S.KeyPressNewPad : S.KeyPressNew;
            _rebind.Start(mode,
                captured => {
                    _listening = false;
                    if (_keymap.Carries(_action, captured)) {
                        _speak(GetFocusText(), true); // already here; nothing changed
                        return;
                    }
                    var holder = _keymap.Holder(captured, _action);
                    if (holder != null) {
                        _speak(SpokenLine.Join(captured.DisplayName, S.KeyAlreadyBound(holder.Label)), true);
                        return;
                    }
                    if (replacing != null) {
                        _keymap.Remove(_action, replacing);
                    }
                    _keymap.Add(_action, captured);
                    _speak(GetFocusText(), true);
                },
                () => {
                    _listening = false;
                    _speak(GetFocusText(), true);
                });
        }

        private void ResetToDefault() {
            _keymap.Reset(_action);
            _speak(SpokenLine.Join(S.SettingReset, Value), true);
        }

        private static string Display(IReadOnlyList<Core.Input.InputBinding> bindings) {
            if (bindings.Count == 0) {
                return S.KeyNotSet;
            }
            var names = new string[bindings.Count];
            for (int i = 0; i < names.Length; i++) {
                names[i] = bindings[i].DisplayName;
            }
            return string.Join(", ", names);
        }
    }
}
