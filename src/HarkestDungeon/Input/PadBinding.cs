using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DD2A11y.Input {
    /// <summary>The gamepad inputs a mod binding can use: buttons, dpad, shoulders, triggers,
    /// stick clicks, and stick directions (the Input System's synthetic press controls).</summary>
    public enum PadInput {
        DpadUp, DpadDown, DpadLeft, DpadRight,
        A, B, X, Y,
        LeftShoulder, RightShoulder,
        LeftTrigger, RightTrigger,
        LeftStickClick, RightStickClick,
        Start, Select,
        LeftStickUp, LeftStickDown, LeftStickLeft, LeftStickRight,
        RightStickUp, RightStickDown, RightStickLeft, RightStickRight,
    }

    /// <summary>
    /// A gamepad combo polled against the Input System's gamepad device, the pad-side sibling of
    /// <see cref="KeyboardBinding"/>. The triggers are the pad's modifier keys: a binding
    /// declares at most one, and trigger state must match exactly so LeftTrigger+A never also
    /// fires a bare-A binding. No device means no match, never a throw.
    /// </summary>
    public sealed class PadBinding : Core.Input.InputBinding {
        private const string Prefix = "pad:";

        private static readonly PadInput[] AllInputs = (PadInput[])System.Enum.GetValues(typeof(PadInput));

        public PadInput Input { get; }
        /// <summary>The trigger held together with <see cref="Input"/>, or null for none.</summary>
        public PadInput? Modifier { get; }

        public PadBinding(PadInput input, PadInput? modifier = null) {
            Input = input;
            Modifier = modifier;
        }

        internal static ButtonControl Control(Gamepad pad, PadInput input) {
            switch (input) {
                case PadInput.DpadUp: return pad.dpad.up;
                case PadInput.DpadDown: return pad.dpad.down;
                case PadInput.DpadLeft: return pad.dpad.left;
                case PadInput.DpadRight: return pad.dpad.right;
                case PadInput.A: return pad.buttonSouth;
                case PadInput.B: return pad.buttonEast;
                case PadInput.X: return pad.buttonWest;
                case PadInput.Y: return pad.buttonNorth;
                case PadInput.LeftShoulder: return pad.leftShoulder;
                case PadInput.RightShoulder: return pad.rightShoulder;
                case PadInput.LeftTrigger: return pad.leftTrigger;
                case PadInput.RightTrigger: return pad.rightTrigger;
                case PadInput.LeftStickClick: return pad.leftStickButton;
                case PadInput.RightStickClick: return pad.rightStickButton;
                case PadInput.Start: return pad.startButton;
                case PadInput.Select: return pad.selectButton;
                case PadInput.LeftStickUp: return pad.leftStick.up;
                case PadInput.LeftStickDown: return pad.leftStick.down;
                case PadInput.LeftStickLeft: return pad.leftStick.left;
                case PadInput.LeftStickRight: return pad.leftStick.right;
                case PadInput.RightStickUp: return pad.rightStick.up;
                case PadInput.RightStickDown: return pad.rightStick.down;
                case PadInput.RightStickLeft: return pad.rightStick.left;
                default: return pad.rightStick.right;
            }
        }

        // Trigger states must match the declared modifier exactly; the input itself never
        // counts as its own modifier, so a bare-trigger binding stays expressible.
        private bool ModifierMatches(Gamepad pad) {
            bool left = pad.leftTrigger.isPressed && Input != PadInput.LeftTrigger;
            bool right = pad.rightTrigger.isPressed && Input != PadInput.RightTrigger;
            if (Modifier == PadInput.LeftTrigger) {
                return left && !right;
            }
            if (Modifier == PadInput.RightTrigger) {
                return right && !left;
            }
            return !left && !right;
        }

        public override bool JustPressed() {
            var pad = Gamepad.current;
            return pad != null && ModifierMatches(pad) && Control(pad, Input).wasPressedThisFrame;
        }

        public override bool Held() {
            var pad = Gamepad.current;
            return pad != null && ModifierMatches(pad) && Control(pad, Input).isPressed;
        }

        public override bool Released() {
            var pad = Gamepad.current;
            return pad != null && ModifierMatches(pad) && Control(pad, Input).wasReleasedThisFrame;
        }

        /// <summary>Whether any pad input was pressed this frame - the whole device, bound or
        /// not. A controller press silences ongoing speech (say-the-spire2's behavior): the
        /// player acted, so what was being said is stale.</summary>
        public static bool AnyJustPressed() {
            var pad = Gamepad.current;
            if (pad == null) {
                return false;
            }
            foreach (var input in AllInputs) {
                if (Control(pad, input).wasPressedThisFrame) {
                    return true;
                }
            }
            return false;
        }

        public override string DisplayName
            => Modifier == null ? Input.ToString() : Modifier + "+" + Input;

        public override string Type => "pad";

        // "pad:A" / "pad:A|LeftTrigger" - the type prefix rides in the serialized form so the
        // one stored [Keys] list can mix keyboard and pad entries.
        public override string Serialize()
            => Modifier == null ? Prefix + Input : Prefix + Input + "|" + Modifier;

        /// <summary>Parse a <see cref="Serialize"/>d combo (prefix included), null when the text
        /// is not a pad combo or does not parse.</summary>
        public static PadBinding TryDeserialize(string text) {
            if (!text.StartsWith(Prefix, System.StringComparison.Ordinal)) {
                return null;
            }
            string body = text.Substring(Prefix.Length);
            string inputPart = body;
            PadInput? modifier = null;
            int bar = body.IndexOf('|');
            if (bar >= 0) {
                inputPart = body.Substring(0, bar);
                if (!System.Enum.TryParse(body.Substring(bar + 1), out PadInput parsedModifier)
                    || (parsedModifier != PadInput.LeftTrigger && parsedModifier != PadInput.RightTrigger)) {
                    return null;
                }
                modifier = parsedModifier;
            }
            if (!System.Enum.TryParse(inputPart, out PadInput input)) {
                return null;
            }
            return new PadBinding(input, modifier);
        }

        /// <summary>The Input System control-path suffix of this binding's input
        /// ("/dpad/up"), for suppressing the game bindings that share it.</summary>
        public string ControlPath {
            get {
                switch (Input) {
                    case PadInput.DpadUp: return "/dpad/up";
                    case PadInput.DpadDown: return "/dpad/down";
                    case PadInput.DpadLeft: return "/dpad/left";
                    case PadInput.DpadRight: return "/dpad/right";
                    case PadInput.A: return "/buttonSouth";
                    case PadInput.B: return "/buttonEast";
                    case PadInput.X: return "/buttonWest";
                    case PadInput.Y: return "/buttonNorth";
                    case PadInput.LeftShoulder: return "/leftShoulder";
                    case PadInput.RightShoulder: return "/rightShoulder";
                    case PadInput.LeftTrigger: return "/leftTrigger";
                    case PadInput.RightTrigger: return "/rightTrigger";
                    case PadInput.LeftStickClick: return "/leftStickPress";
                    case PadInput.RightStickClick: return "/rightStickPress";
                    case PadInput.Start: return "/start";
                    case PadInput.Select: return "/select";
                    case PadInput.LeftStickUp: return "/leftStick/up";
                    case PadInput.LeftStickDown: return "/leftStick/down";
                    case PadInput.LeftStickLeft: return "/leftStick/left";
                    case PadInput.LeftStickRight: return "/leftStick/right";
                    case PadInput.RightStickUp: return "/rightStick/up";
                    case PadInput.RightStickDown: return "/rightStick/down";
                    case PadInput.RightStickLeft: return "/rightStick/left";
                    default: return "/rightStick/right";
                }
            }
        }

        /// <summary>Every input currently pressed on the device (the listen's initial-held
        /// snapshot).</summary>
        internal static System.Collections.Generic.List<PadInput> HeldNow() {
            var held = new System.Collections.Generic.List<PadInput>();
            var pad = Gamepad.current;
            if (pad == null) {
                return held;
            }
            foreach (var input in AllInputs) {
                if (Control(pad, input).isPressed) {
                    held.Add(input);
                }
            }
            return held;
        }

        internal static PadInput[] All => AllInputs;
    }
}
