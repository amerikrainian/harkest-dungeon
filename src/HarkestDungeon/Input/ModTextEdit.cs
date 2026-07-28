using System;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DD2A11y.Input {
    /// <summary>
    /// Mod-owned text entry, for fields the game has no widget behind (the mod settings tab).
    /// Characters arrive through the keyboard's text-input event - layout-aware, so Shift, dead
    /// keys, and AltGr chords come out right, which is also why control chords need no gate
    /// here: they surface as control characters and are filtered. Enter commits, Escape
    /// cancels, Backspace erases, each echoed. The IME is enabled for the session so CJK
    /// composition works on a mod-drawn field; while a composition is open, Enter and Escape
    /// accept or cancel the composition (the committed characters then arrive as ordinary text
    /// input), not the edit. While an edit is active the input registry stands down the same
    /// way it does for the game's own text fields, and the session dies with its owner: a
    /// screen change ends it silently, so keys are never routed into a dead field.
    /// </summary>
    public sealed class ModTextEdit {
        private readonly Action<string, bool> _speak;
        private readonly Func<object> _owner;
        private Action<string> _commit;
        private Action _cancel;
        private string _buffer = "";
        private Keyboard _keyboard;
        private object _ownerAtBegin;
        private bool _composing;

        public bool Active { get; private set; }

        public ModTextEdit(Action<string, bool> speak, Func<object> owner) {
            _speak = speak;
            _owner = owner;
        }

        /// <summary>Start an edit with an empty buffer (matching the game's own edit flow, which
        /// clears the field it opens). False when no keyboard device exists.</summary>
        public bool Begin(Action<string> commit, Action cancel) {
            _keyboard = Keyboard.current;
            if (_keyboard == null) {
                Plugin.Log.LogWarning("text edit: no keyboard device; edit not started");
                return false;
            }
            _commit = commit;
            _cancel = cancel;
            _buffer = "";
            _ownerAtBegin = _owner();
            _composing = false;
            Active = true;
            _keyboard.onTextInput += OnChar;
            _keyboard.onIMECompositionChange += OnComposition;
            _keyboard.SetIMEEnabled(true);
            _speak(S.EditStarted, true);
            return true;
        }

        private void OnChar(char c) {
            if (!Active || char.IsControl(c)) {
                return;
            }
            _buffer += c;
            _speak(SpokenChars.Name(c), true);
        }

        private void OnComposition(IMECompositionString composition) {
            _composing = composition.Count > 0;
        }

        /// <summary>Ticked from the pump every frame, before the input registry.</summary>
        public void Tick() {
            if (!Active) {
                return;
            }
            // The owner screen went away (closed, replaced): the field is gone, end silently -
            // the new surface has already announced itself.
            if (!ReferenceEquals(_owner(), _ownerAtBegin)) {
                End();
                return;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                End();
                _cancel();
                return;
            }
            if (_composing) {
                return;
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) {
                string text = _buffer;
                End();
                _commit(text);
            } else if (keyboard.escapeKey.wasPressedThisFrame) {
                End();
                _cancel();
            } else if (keyboard.backspaceKey.wasPressedThisFrame && _buffer.Length > 0) {
                char erased = _buffer[_buffer.Length - 1];
                _buffer = _buffer.Substring(0, _buffer.Length - 1);
                _speak(S.EditDeleted(SpokenChars.Name(erased)), true);
            }
        }

        private void End() {
            Active = false;
            _keyboard.onTextInput -= OnChar;
            _keyboard.onIMECompositionChange -= OnComposition;
            _keyboard.SetIMEEnabled(false);
            _keyboard = null;
        }
    }
}
