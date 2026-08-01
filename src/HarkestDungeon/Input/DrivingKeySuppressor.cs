using System.Collections.Generic;
using Assets.Code.Inputs;
using Assets.Code.Utils;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DD2A11y.Input {
    /// <summary>
    /// Disables chosen keyboard bindings in the game's input asset with empty binding overrides
    /// while a shared-keyboard screen stands, restoring them when it leaves. The road map claims
    /// the arrow keys and bare Ctrl (its hold-to-show token glossary) so WASD keeps driving the
    /// coach while the map is browsed; the driving screen claims Tab for its whole stand and the
    /// list keys only while focus is off the driving area. Composite ctrl+key combos stay live
    /// except for their claimed-key halves. Re-asserted every frame - the game rebuilds input
    /// state behind our back on device and mode changes.
    /// </summary>
    public sealed class DrivingKeySuppressor {
        private static readonly AccessTools.FieldRef<InputSystemBhv, InputActionAsset> AssetField =
            AccessTools.FieldRefAccess<InputSystemBhv, InputActionAsset>("m_defaultInputActions");

        private static readonly string[] ArrowPaths = {
            "/upArrow", "/downArrow", "/leftArrow", "/rightArrow",
        };

        // Applying or removing a binding override re-resolves the whole action state, and the
        // game's InputSystem re-fires still-held Button actions in the process: the G that
        // summoned the goals panel toggled it a second time the moment the focus jump engaged
        // the off-area claim. Transitions therefore wait out any held toggle-class key - the
        // per-frame callers retry once it lifts. WASD and arrow holds do not defer: their
        // continuous driving actions re-fire harmlessly.
        private static readonly Key[] TransitionUnsafeKeys = {
            Key.G, Key.M, Key.Z, Key.I, Key.C, Key.E, Key.Q, Key.R, Key.X, Key.V, Key.Y,
            Key.T, Key.Space, Key.Enter, Key.NumpadEnter, Key.Escape, Key.LeftAlt, Key.RightAlt,
        };

        private readonly string[] _keyPaths;
        private readonly bool _bareCtrl;
        private readonly bool _navigationEvents;
        private readonly List<(InputAction Action, int Index)> _overridden = new List<(InputAction, int)>();
        private bool _active;

        /// <summary>The road map's claim: the arrow keys and bare Ctrl, uGUI navigation
        /// silenced.</summary>
        public DrivingKeySuppressor() : this(ArrowPaths, bareCtrl: true, navigationEvents: true) { }

        /// <summary>Claim the bindings whose paths end in <paramref name="keyPaths"/> ("/tab");
        /// non-composite bare-Ctrl bindings and uGUI navigation events join the claim when
        /// asked.</summary>
        public DrivingKeySuppressor(string[] keyPaths, bool bareCtrl, bool navigationEvents) {
            _keyPaths = keyPaths;
            _bareCtrl = bareCtrl;
            _navigationEvents = navigationEvents;
        }

        public void Reassert() {
            _active = true;
            if (_navigationEvents) {
                var eventSystem = EventSystem.current;
                if (eventSystem != null) {
                    eventSystem.sendNavigationEvents = false;
                }
            }
            if (_overridden.Count > 0 && IsStillOverridden(_overridden[0])) {
                return;
            }
            if (TransitionUnsafe()) {
                return; // deferred; the per-frame reassert retries once the key lifts
            }
            _overridden.Clear();
            var asset = Asset();
            if (asset == null) {
                return;
            }
            foreach (var map in asset.actionMaps) {
                foreach (var action in map.actions) {
                    var bindings = action.bindings;
                    for (int i = 0; i < bindings.Count; i++) {
                        string path = bindings[i].effectivePath;
                        if (path == null) {
                            continue;
                        }
                        if (!IsClaimedKey(path) && !(_bareCtrl && IsBareCtrl(path) && !bindings[i].isPartOfComposite)) {
                            continue;
                        }
                        action.ApplyBindingOverride(i, string.Empty);
                        _overridden.Add((action, i));
                    }
                }
            }
            if (_overridden.Count == 0) {
                Plugin.Log.LogWarning("suppressor: no bindings found for "
                    + string.Join(" ", _keyPaths) + "; those game keys will fight ours");
            }
        }

        /// <summary>Release the claim. Per-frame callers pass <paramref name="immediate"/>
        /// false so the removal also waits out a held toggle key; one-shot callers (a
        /// screen's leave) restore unconditionally.</summary>
        public void Restore(bool immediate = true) {
            if (!_active) {
                return;
            }
            if (!immediate && TransitionUnsafe()) {
                return; // deferred; the per-frame caller retries once the key lifts
            }
            _active = false;
            foreach (var (action, index) in _overridden) {
                action.RemoveBindingOverride(index);
            }
            _overridden.Clear();
            if (_navigationEvents) {
                var eventSystem = EventSystem.current;
                if (eventSystem != null) {
                    eventSystem.sendNavigationEvents = true;
                }
            }
        }

        private static bool IsStillOverridden((InputAction Action, int Index) entry)
            => entry.Index < entry.Action.bindings.Count
               && entry.Action.bindings[entry.Index].overridePath == string.Empty;

        private static bool TransitionUnsafe() {
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                return false;
            }
            foreach (var key in TransitionUnsafeKeys) {
                if (keyboard[key].isPressed) {
                    return true;
                }
            }
            return false;
        }

        private bool IsClaimedKey(string path) {
            foreach (var key in _keyPaths) {
                if (path.EndsWith(key, System.StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBareCtrl(string path)
            => path.EndsWith("/ctrl", System.StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/leftCtrl", System.StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/rightCtrl", System.StringComparison.OrdinalIgnoreCase);

        private static InputActionAsset Asset() {
            if (!SingletonMonoBehaviour<InputSystemBhv>.HasInstance()) {
                return null;
            }
            return AssetField(SingletonMonoBehaviour<InputSystemBhv>.Instance);
        }
    }
}
