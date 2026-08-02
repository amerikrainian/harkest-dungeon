using System;
using System.Collections.Generic;
using Assets.Code.Inputs;
using Assets.Code.Utils;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DD2A11y.Input {
    /// <summary>
    /// Disables chosen keyboard bindings in the game's input asset with empty binding overrides
    /// while a shared-keyboard screen stands, restoring them when it leaves. The claim is a
    /// PROVIDER over the mod's live bindings, re-evaluated on every reassert: rebinding a mod
    /// key moves the suppression with it the same frame, handing the freed key back to the game
    /// (Tab's minimap returns when panel cycling moves elsewhere). Composite ctrl+key combos
    /// stay live except for their claimed-key halves. Re-asserted every frame - the game
    /// rebuilds input state behind our back on device and mode changes.
    /// </summary>
    public sealed class DrivingKeySuppressor {
        private static readonly AccessTools.FieldRef<InputSystemBhv, InputActionAsset> AssetField =
            AccessTools.FieldRefAccess<InputSystemBhv, InputActionAsset>("m_defaultInputActions");

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

        private readonly Func<(string[] KeyPaths, bool BareCtrl)> _claim;
        private readonly bool _navigationEvents;
        private readonly List<(InputAction Action, int Index)> _overridden = new List<(InputAction, int)>();
        private string[] _keyPaths = Array.Empty<string>();
        private bool _bareCtrl;
        private bool _scanned; // a scan ran for the applied claim, even one that found nothing
        private bool _active;

        /// <summary>Claim the game bindings on the keys the provider names ("/tab"), plus
        /// non-composite bare-Ctrl bindings when the claim asks; uGUI navigation events join
        /// when <paramref name="navigationEvents"/>.</summary>
        public DrivingKeySuppressor(Func<(string[] KeyPaths, bool BareCtrl)> claim, bool navigationEvents) {
            _claim = claim;
            _navigationEvents = navigationEvents;
        }

        /// <summary>A claim over the LIVE bindings of the named mod actions: one path per bound
        /// key, bare Ctrl joined when any of them chords with Ctrl (the game's Ctrl is a hold).
        /// Evaluated per reassert, so a rebind moves the suppression with the key.</summary>
        public static (string[] KeyPaths, bool BareCtrl) ClaimFor(
            Core.Input.InputManager input, params string[] actionKeys) {
            var paths = new List<string>();
            bool bareCtrl = false;
            foreach (var action in input.Actions) {
                if (Array.IndexOf(actionKeys, action.Key) < 0) {
                    continue;
                }
                foreach (var binding in action.Bindings) {
                    if (!(binding is KeyboardBinding keyboard)) {
                        continue;
                    }
                    bareCtrl |= keyboard.Ctrl;
                    if (!paths.Contains(keyboard.ControlPath)) {
                        paths.Add(keyboard.ControlPath);
                    }
                }
            }
            return (paths.ToArray(), bareCtrl);
        }

        public void Reassert() {
            _active = true;
            if (_navigationEvents) {
                var eventSystem = EventSystem.current;
                if (eventSystem != null) {
                    eventSystem.sendNavigationEvents = false;
                }
            }
            var (keyPaths, bareCtrl) = _claim();
            bool claimChanged = bareCtrl != _bareCtrl || !SamePaths(keyPaths);
            if (!claimChanged && _scanned
                && (_overridden.Count == 0 || IsStillOverridden(_overridden[0]))) {
                return;
            }
            if (TransitionUnsafe()) {
                return; // deferred; the per-frame reassert retries once the key lifts
            }
            // A claim change (a rebind) swaps the whole override set: the old claim's keys go
            // back to the game before the new claim's are taken.
            foreach (var entry in _overridden) {
                if (IsStillOverridden(entry)) {
                    entry.Action.RemoveBindingOverride(entry.Index);
                }
            }
            _overridden.Clear();
            _keyPaths = keyPaths;
            _bareCtrl = bareCtrl;
            _scanned = true;
            var asset = Asset();
            if (asset == null) {
                _scanned = false; // the game's input singleton is not up yet; retry next frame
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
            if (_overridden.Count == 0 && _keyPaths.Length > 0) {
                Plugin.Log.LogInfo("suppressor: no game bindings on "
                    + string.Join(" ", _keyPaths) + "; nothing to rest");
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
            _scanned = false;
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

        private bool SamePaths(string[] paths) {
            if (paths.Length != _keyPaths.Length) {
                return false;
            }
            for (int i = 0; i < paths.Length; i++) {
                if (paths[i] != _keyPaths[i]) {
                    return false;
                }
            }
            return true;
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
                if (path.EndsWith(key, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBareCtrl(string path)
            => path.EndsWith("/ctrl", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/leftCtrl", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/rightCtrl", StringComparison.OrdinalIgnoreCase);

        private static InputActionAsset Asset() {
            if (!SingletonMonoBehaviour<InputSystemBhv>.HasInstance()) {
                return null;
            }
            return AssetField(SingletonMonoBehaviour<InputSystemBhv>.Instance);
        }
    }
}
