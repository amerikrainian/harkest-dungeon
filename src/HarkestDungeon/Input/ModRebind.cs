using System;
using UnityEngine.InputSystem;

namespace DD2A11y.Input {
    /// <summary>
    /// The mod-keys tab's key capture: while listening, every mod key pauses (wired into the
    /// input manager's suppression, same as text entry) and the next non-modifier key pressed -
    /// with whatever Ctrl/Shift/Alt are held at that moment, so chords capture naturally -
    /// becomes the binding. Escape keeps the current binding. Ticked from the pump, ahead of
    /// the input manager.
    /// </summary>
    public sealed class ModRebind {
        private Action<KeyboardBinding> _onCaptured;
        private Action _onCancelled;

        public bool Active { get; private set; }

        public void Start(Action<KeyboardBinding> onCaptured, Action onCancelled) {
            Active = true;
            _onCaptured = onCaptured;
            _onCancelled = onCancelled;
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
            if (keyboard == null) {
                return;
            }
            if (keyboard.escapeKey.wasPressedThisFrame) {
                var cancelled = _onCancelled;
                Abort();
                cancelled?.Invoke();
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
                var captured = _onCaptured;
                Abort();
                captured?.Invoke(binding);
                return;
            }
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
