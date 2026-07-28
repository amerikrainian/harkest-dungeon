using UnityEngine.InputSystem;

namespace DD2A11y.Input {
    /// <summary>
    /// A keyboard combo polled against the Unity Input System's keyboard device (the game ships
    /// with Input System handling ONLY - the legacy UnityEngine.Input API throws). Device-level
    /// state is read directly, so the game's action maps being disabled by the input gate does
    /// not affect us. Modifiers must match exactly so Ctrl+Up does not also fire a bare-Up
    /// binding. The concrete binding behind Core's registry (base type spelled out - the Input
    /// System namespace has its own InputBinding).
    /// </summary>
    public sealed class KeyboardBinding : Core.Input.InputBinding {
        public Key Key { get; }
        public bool Ctrl { get; }
        public bool Shift { get; }
        public bool Alt { get; }

        public KeyboardBinding(Key key, bool ctrl = false, bool shift = false, bool alt = false) {
            Key = key;
            Ctrl = ctrl;
            Shift = shift;
            Alt = alt;
        }

        private static bool CtrlHeld(Keyboard kb) => kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
        private static bool ShiftHeld(Keyboard kb) => kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        private static bool AltHeld(Keyboard kb) => kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;

        private bool ModifiersMatch(Keyboard kb)
            => Ctrl == CtrlHeld(kb) && Shift == ShiftHeld(kb) && Alt == AltHeld(kb);

        public override bool JustPressed() {
            var kb = Keyboard.current;
            return kb != null && ModifiersMatch(kb) && kb[Key].wasPressedThisFrame;
        }

        public override bool Held() {
            var kb = Keyboard.current;
            return kb != null && ModifiersMatch(kb) && kb[Key].isPressed;
        }

        public override bool Released() {
            var kb = Keyboard.current;
            return kb != null && ModifiersMatch(kb) && kb[Key].wasReleasedThisFrame;
        }

        public override string DisplayName {
            get {
                var s = "";
                if (Ctrl) {
                    s += "Ctrl+";
                }
                if (Shift) {
                    s += "Shift+";
                }
                if (Alt) {
                    s += "Alt+";
                }
                return s + Key;
            }
        }

        public override string Type => KeyboardType;

        // "UpArrow|ctrl,shift" - the Key, then a comma-list of held modifiers (omitted if none).
        public override string Serialize() {
            var mods = new System.Collections.Generic.List<string>();
            if (Ctrl) {
                mods.Add("ctrl");
            }
            if (Shift) {
                mods.Add("shift");
            }
            if (Alt) {
                mods.Add("alt");
            }
            return mods.Count == 0 ? Key.ToString() : Key + "|" + string.Join(",", mods);
        }
    }
}
