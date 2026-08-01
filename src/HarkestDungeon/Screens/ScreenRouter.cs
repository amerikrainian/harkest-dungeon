using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Input;

namespace DD2A11y.Screens {
    /// <summary>
    /// Resolves the active surface once per frame, in registration order (modals first, then
    /// stack screens, then game-mode screens). On a match it takes the keyboard (input gate),
    /// builds the screen tree, attaches the navigator, and speaks the screen name then the
    /// landing focus; when nothing matches the keyboard is released. While a screen stands, its
    /// OnUpdate runs and a focus orphaned by a rebuild is re-homed and re-announced here - the
    /// single announce chokepoint.
    /// </summary>
    public sealed class ScreenRouter {
        private readonly List<GameScreen> _screens = new List<GameScreen>();
        private readonly TraditionalNavigator _navigator;
        private readonly InputGate _gate;
        private readonly Action<string, bool> _speak;

        private GameScreen _active;
        private object _target;

        public ScreenRouter(TraditionalNavigator navigator, InputGate gate, Action<string, bool> speak) {
            _navigator = navigator;
            _gate = gate;
            _speak = speak;
        }

        public TraditionalNavigator Navigator => _navigator;
        public GameScreen Active => _active;
        public bool HasScreen => _active != null;

        public void Register(GameScreen screen) => _screens.Add(screen);

        public void Tick() {
            GameScreen match = null;
            object target = null;
            for (int i = 0; i < _screens.Count; i++) {
                target = _screens[i].ResolveTarget();
                if (target != null) {
                    match = _screens[i];
                    break;
                }
            }

            if (match == null) {
                if (_active != null) {
                    Leave();
                }
                return;
            }

            if (match != _active || !ReferenceEquals(target, _target)) {
                Enter(match, target);
                return;
            }

            bool announceRequested = _active.OnUpdate(_target);
            if (_navigator.EnsureFocusValid() || announceRequested) {
                // Skipped when the re-landed line matches the last announcement verbatim - a
                // screen the game populates a beat after entry rebuilds on every open.
                _navigator.AnnounceCurrentIfChanged();
            }
        }

        private void Enter(GameScreen screen, object target) {
            if (_active != null && _active != screen) {
                _active.OnLeave();
            }
            _active = screen;
            _target = target;
            Plugin.Log.LogInfo("screen: " + screen.GetType().Name);
            if (screen.CapturesKeyboard) {
                _gate.Capture();
            } else {
                _gate.Release();
            }
            _navigator.Attach(screen.BuildRoot(target));
            _speak(screen.Name, true);
            _navigator.AnnounceCurrent();
        }

        private void Leave() {
            Plugin.Log.LogInfo("screen: none (released)");
            _active.OnLeave();
            _active = null;
            _target = null;
            _navigator.Attach(null);
            _gate.Release();
        }

        /// <summary>Dev-server readout of where the mod thinks it is.</summary>
        public string Describe() {
            if (_active == null) {
                return "no screen";
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("screen: ").Append(_active.GetType().Name).Append(" (").Append(_active.Name).Append(")\n");
            sb.Append("focus:");
            foreach (var element in _navigator.FocusPath) {
                sb.Append(" > ").Append(element.GetType().Name).Append("[").Append(element.GetFocusText()).Append("]");
            }
            return sb.ToString();
        }
    }
}
