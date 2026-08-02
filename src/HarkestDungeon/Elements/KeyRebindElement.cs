using System.Collections.Generic;
using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One mod-keys row: the command's name and its current key or keys. Enter listens for the
    /// new key (the next non-modifier press, chord-aware; Escape keeps the current one) and
    /// reads back the result, naming any command the key was pulled off. Shift+Enter (the
    /// discard action) restores the command's default keys. The buffer carries the default.
    /// </summary>
    public sealed class KeyRebindElement : UIElement {
        private readonly InputAction _action;
        private readonly ModKeymap _keymap;
        private readonly ModRebind _rebind;
        private readonly System.Action<string, bool> _speak;

        public KeyRebindElement(InputAction action, ModKeymap keymap, ModRebind rebind,
                                System.Action<string, bool> speak) {
            _action = action;
            _keymap = keymap;
            _rebind = rebind;
            _speak = speak;
        }

        public override string Label => _action.Label;

        public override string Value => Display(_action.Bindings);

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, StartListen);
            yield return new ElementAction("discard", ResetToDefault);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            yield return S.KeyDefault(Display(_keymap.DefaultsOf(_action)));
        }

        private void StartListen() {
            _speak(S.KeyPressNew, true);
            _rebind.Start(
                captured => {
                    var displaced = _keymap.Rebind(_action, captured);
                    string line = Value;
                    foreach (var other in displaced) {
                        line = SpokenLine.Join(line, S.KeyTakenFrom(other.Label));
                    }
                    _speak(line, true);
                },
                () => _speak(GetFocusText(), true));
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
