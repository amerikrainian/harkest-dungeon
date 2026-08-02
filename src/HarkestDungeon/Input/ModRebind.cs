using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DD2A11y.Input {
    /// <summary>What device a listen captures from; the other device is ignored so a keyboard
    /// row never captures a stray pad press and vice versa (Escape always cancels).</summary>
    public enum ListenMode { Keyboard, Pad }

    /// <summary>
    /// The mod-keys tab's key capture: while listening, every mod key pauses (wired into the
    /// input manager's suppression, same as text entry). A keyboard listen captures the next
    /// non-modifier key pressed, with whatever Ctrl/Shift/Alt are held at that moment, so
    /// chords capture naturally. A pad listen captures on RELEASE (say-the-spire2's model) so a
    /// trigger can be held first as the modifier: the released input is the binding, a trigger
    /// still held is its modifier, and inputs already held when the listen started are ignored
    /// on their release. Escape keeps things as they are. Ticked from the pump, ahead of the
    /// input manager.
    /// </summary>
    public sealed class ModRebind {
        private Action<Core.Input.InputBinding> _onCaptured;
        private Action _onCancelled;
        private ListenMode _mode;
        private List<PadInput> _initialHeld = new List<PadInput>();

        public bool Active { get; private set; }

        public void Start(ListenMode mode, Action<Core.Input.InputBinding> onCaptured, Action onCancelled) {
            Active = true;
            _mode = mode;
            _onCaptured = onCaptured;
            _onCancelled = onCancelled;
            if (mode == ListenMode.Pad) {
                _initialHeld = PadBinding.HeldNow();
            }
        }

        /// <summary>End a dangling listen without a word (the tab was hidden under it).</summary>
        public void Abort() {
            Active = false;
            _onCaptured = null;
            _onCancelled = null;
        }

        public void Tick() {
            if (!Active) {
                return;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) {
                var cancelled = _onCancelled;
                Abort();
                cancelled?.Invoke();
                return;
            }
            if (_mode == ListenMode.Keyboard) {
                TickKeyboard(keyboard);
            } else {
                TickPad();
            }
        }

        private void TickKeyboard(Keyboard keyboard) {
            if (keyboard == null) {
                return;
            }
            foreach (var control in keyboard.allKeys) {
                if (!control.wasPressedThisFrame || IsModifier(control.keyCode)) {
                    continue;
                }
                var binding = new KeyboardBinding(control.keyCode,
                    keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed,
                    keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
                    keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
                Capture(binding);
                return;
            }
        }

        private void TickPad() {
            var pad = Gamepad.current;
            if (pad == null) {
                return;
            }
            foreach (var input in PadBinding.All) {
                if (!PadBinding.Control(pad, input).wasReleasedThisFrame) {
                    continue;
                }
                if (_initialHeld.Remove(input)) {
                    continue; // held before the listen started; its release is not a choice
                }
                PadInput? modifier = null;
                if (input != PadInput.LeftTrigger && pad.leftTrigger.isPressed) {
                    modifier = PadInput.LeftTrigger;
                } else if (input != PadInput.RightTrigger && pad.rightTrigger.isPressed) {
                    modifier = PadInput.RightTrigger;
                }
                Capture(new PadBinding(input, modifier));
                return;
            }
        }

        private void Capture(Core.Input.InputBinding binding) {
            var captured = _onCaptured;
            Abort();
            captured?.Invoke(binding);
        }

        // Modifiers are chord parts, never a binding of their own; the OS keys stay the OS's.
        private static bool IsModifier(Key key) {
            switch (key) {
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftMeta:
                case Key.RightMeta:
                    return true;
                default:
                    return false;
            }
        }
    }
}
