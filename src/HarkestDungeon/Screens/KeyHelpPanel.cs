using System;
using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// "Keys here" (F1, anywhere): the keys that would do something right now, for the screen
    /// and the element the player stood on when they asked (<see cref="KeyHelp"/>: the combat
    /// glances on the battlefield, discard on a discardable item, a run glance only inside a
    /// run, Escape only where it closes something), each row what the key does and the keys
    /// bound to it. The list is a popup over the screen's tree, so the screen's focus is saved
    /// and restored around it, and it is taken the moment the help opens: once it is up, the
    /// focused element is its own. While it is up only its own keys are live (navigation, the
    /// buffers, F1, the stand-down), and the keyboard is the mod's even over a screen that
    /// shares it with the game (the road).
    /// <para>Enter on a row runs the key as a press would: the help closes silently, focus is
    /// back where it was, the key is dispatched through the real input path on the next frame,
    /// and then the focus is read again, since the key is likely to have changed what stands
    /// there. What is heard, in order: whatever the key itself says, then the focused element;
    /// never the key's name, which the player has just read off the row. A speech hold keeps
    /// that order (its first line interrupts, the rest queue). The focus is not read again when
    /// the key moved it (the move spoke it), changed the screen or opened a popup (both
    /// announce themselves), or when the key's own feedback already said it.</para>
    /// </summary>
    public sealed class KeyHelpPanel {
        public const string ActionKey = "mod.help";

        // Frames from dispatching to reading the focus: past the feedback a key defers to the
        // game's next frames (a rebuild's re-landing, a refreshed value).
        private const int FocusAfter = 16;

        private readonly InputManager _input;
        private readonly TraditionalNavigator _navigator;
        private readonly ScreenRouter _router;
        private readonly InputGate _gate;
        private readonly SpeechPipeline _speech;

        private Popup _popup;
        private bool _tookGate;
        // The screen the help opened over, the row's key waiting for the next frame, the focus
        // that key ran against, and the frames left until the focus is read again (0 = none due).
        private GameScreen _screen;
        private InputAction _pending;
        private UIElement _before;
        private int _readIn;

        public KeyHelpPanel(InputManager input, TraditionalNavigator navigator, ScreenRouter router,
                            InputGate gate, SpeechPipeline speech) {
            _input = input;
            _navigator = navigator;
            _router = router;
            _gate = gate;
            _speech = speech;
        }

        public bool Open => _popup != null;

        /// <summary>Whether a key stays live while the help is up: the navigation the list
        /// needs, buffer review, the help key itself, and the stand-down.</summary>
        public static bool KeepsLive(InputAction action) {
            switch (action.Key) {
                case UiActions.Up:
                case UiActions.Down:
                case UiActions.Left:
                case UiActions.Right:
                case UiActions.Next:
                case UiActions.Prev:
                case UiActions.Home:
                case UiActions.End:
                case UiActions.Activate:
                case UiActions.Back:
                case ActionKey:
                case "mod.standdown":
                    return true;
                default:
                    return action.Key.StartsWith("buffer.", StringComparison.Ordinal);
            }
        }

        /// <summary>F1: open the help, or close it again (the restored focus reads, as Escape's
        /// close does).</summary>
        public void Toggle() {
            if (Open) {
                _navigator.ClosePopup();
                return;
            }
            Show();
        }

        /// <summary>Close without a word, for a caller whose own line follows (the stand-down).</summary>
        public void Dismiss() {
            if (Open) {
                _navigator.ClosePopup(announce: false);
            }
        }

        private void Show() {
            // A popup already up (a dropdown's choices) closes first: the help lists the keys of
            // the element that opened it, which is where the help returns.
            if (_navigator.PopupOpen) {
                _navigator.ClosePopup(announce: false);
            }
            var actions = KeyHelp.Collect(_input.Actions, _input.IsLive, DispatchDryRun,
                message => Plugin.Log.LogWarning(message), action => action.Key == ActionKey);
            _screen = _router.Active;
            var root = new Container(ContainerShape.VerticalList, S.InputHelp);
            if (actions.Count == 0) {
                root.Add(new ReadoutElement(() => S.HelpNone));
            }
            foreach (var action in actions) {
                var a = action;
                root.Add(new ActionElement(() => S.HelpRow(a.Label, KeyHelp.KeysOf(a, Shown)), null, () => Run(a)));
            }
            if (!_gate.Captured) {
                _gate.Capture();
                _tookGate = true;
            }
            _popup = new Popup(root, OnClosed);
            _navigator.OpenPopup(_popup);
        }

        // Every close path lands here: Escape, F1, a row's Enter, a screen change under the help.
        private void OnClosed() {
            _popup = null;
            if (!_tookGate) {
                return;
            }
            _tookGate = false;
            // The keyboard goes back to the game only where the help took it from the game: a
            // screen sharing it (the road), or no screen at all.
            if (_router.Active == null || !_router.Active.CapturesKeyboard) {
                _gate.Release();
            }
        }

        // The pad's combos are read only while a pad is connected.
        private static bool Shown(InputBinding binding)
            => !(binding is PadBinding) || UnityEngine.InputSystem.Gamepad.current != null;

        // Runtime's key dispatcher, decided without acting: a UI key goes to the screen first,
        // then the navigator; anything they leave falls to the action's own handler.
        private bool? DispatchDryRun(InputAction action) {
            if (!action.Key.StartsWith("ui.", StringComparison.Ordinal) || !_router.HasScreen) {
                return null;
            }
            bool? screen = _router.Active.DryRunAction(action.Key);
            if (screen != null) {
                return screen;
            }
            return _navigator.WouldHandle(action.Key) ? true : (bool?)null;
        }

        // Enter on a row. Nothing is said for the Enter itself: the player has just read the
        // row. The hold's first line (the key's own, else the focus) cuts the row's readout off.
        // The key waits a frame, so the navigator finishes this Enter on a closed popup before
        // the key can open one of its own.
        private void Run(InputAction action) {
            _speech.BeginHold();
            _navigator.ClosePopup(announce: false);
            _pending = action;
        }

        /// <summary>Ticked once per frame from the pump, ahead of the key poll.</summary>
        public void Tick() {
            if (_pending != null) {
                var action = _pending;
                _pending = null;
                Dispatch(action);
                return;
            }
            if (_readIn > 0 && --_readIn == 0) {
                ReadFocus();
            }
        }

        private void Dispatch(InputAction action) {
            if (_router.Active != _screen) {
                _speech.EndHold(); // the screen changed meanwhile and announces itself
                return;
            }
            _before = _navigator.Current;
            _input.FireAction(action.Key);
            _readIn = FocusAfter;
        }

        private void ReadFocus() {
            try {
                // A real key press since then ended the hold: the player has moved on.
                if (!_speech.Holding || _router.Active != _screen || _navigator.PopupOpen) {
                    return;
                }
                var current = _navigator.Current;
                if (current == null || current != _before || AlreadySaid(current.GetBufferHeadText())) {
                    return;
                }
                _speech.Speak(current.GetFocusText());
            } finally {
                _speech.EndHold();
                _before = null;
            }
        }

        // Whether the key's own feedback read the focused element already.
        private bool AlreadySaid(string head) {
            string clean = TextFilter.Clean(head);
            if (clean.Length == 0) {
                return false;
            }
            foreach (var said in _speech.Held) {
                if (said.IndexOf(clean, StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
            }
            return false;
        }
    }
}
