using Assets.Code.Game;
using Assets.Code.Inputs;
using Assets.Code.Utils;
using UnityEngine.EventSystems;

namespace DD2A11y.Input {
    /// <summary>
    /// Takes the keyboard from the game while a supported screen is up: uGUI navigation events are
    /// suppressed (so our arrows do not also drive the game's selection) and the game's own input
    /// action maps are disabled (so Enter does not also fire the game's Submit listeners, Escape its
    /// ExitMenu). Both are re-asserted EVERY captured frame - the game's own dialog teardown
    /// re-enables sendNavigationEvents behind our back. Mouse/pointer input is untouched.
    /// </summary>
    public sealed class InputGate {
        // The always-on maps the game's global listeners live in; the current game mode's own map
        // (e.g. "MainMenu", "Driving") is added per frame.
        private static readonly string[] BaseMaps = { "Default", "UI" };

        public bool Captured { get; private set; }

        public void Capture() {
            Captured = true;
        }

        public void Release() {
            if (!Captured) {
                return;
            }
            Captured = false;
            SetGameInput(enabled: true);
            var eventSystem = EventSystem.current;
            if (eventSystem != null) {
                eventSystem.sendNavigationEvents = true;
            }
        }

        /// <summary>Called every frame while captured (idempotent).</summary>
        public void Reassert() {
            if (!Captured) {
                return;
            }
            var eventSystem = EventSystem.current;
            if (eventSystem != null) {
                eventSystem.sendNavigationEvents = false;
            }
            SetGameInput(enabled: false);
        }

        private static void SetGameInput(bool enabled) {
            if (!SingletonMonoBehaviour<InputSystemBhv>.HasInstance()) {
                return;
            }
            var input = SingletonMonoBehaviour<InputSystemBhv>.Instance;
            foreach (var map in BaseMaps) {
                input.SetInputActionMapEnabled(map, enabled);
            }
            var mode = GameModeMgr.CurrentMode;
            if (mode != null && !string.IsNullOrEmpty(mode.m_inputMapName)) {
                input.SetInputActionMapEnabled(mode.m_inputMapName, enabled);
            }
        }
    }
}
