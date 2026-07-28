using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Inputs;
using Assets.Code.Utils;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DD2A11y.Input {
    /// <summary>
    /// The map screen's half of the keyboard: it claims the arrow keys and the Ctrl chords, so
    /// WASD keeps driving the coach while the map is browsed. The game binds arrows and WASD to
    /// the same driving actions, and bare Ctrl is its hold-to-show token glossary
    /// (UI/TokenReferenceView) - both are disabled with empty binding overrides while the map
    /// screen stands (and uGUI navigation is silenced), restored when it leaves. Composite
    /// ctrl+key combos stay live except for their arrow halves. Re-asserted every frame - the
    /// game rebuilds input state behind our back on device and mode changes.
    /// </summary>
    public sealed class DrivingKeySuppressor {
        private static readonly AccessTools.FieldRef<InputSystemBhv, InputActionAsset> AssetField =
            AccessTools.FieldRefAccess<InputSystemBhv, InputActionAsset>("m_defaultInputActions");

        private static readonly string[] ArrowPaths = {
            "/upArrow", "/downArrow", "/leftArrow", "/rightArrow",
        };

        private readonly List<(InputAction Action, int Index)> _overridden = new List<(InputAction, int)>();
        private bool _active;

        public void Reassert() {
            _active = true;
            var eventSystem = EventSystem.current;
            if (eventSystem != null) {
                eventSystem.sendNavigationEvents = false;
            }
            if (_overridden.Count > 0 && IsStillOverridden(_overridden[0])) {
                return;
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
                        if (!IsArrow(path) && !(IsBareCtrl(path) && !bindings[i].isPartOfComposite)) {
                            continue;
                        }
                        action.ApplyBindingOverride(i, string.Empty);
                        _overridden.Add((action, i));
                    }
                }
            }
            if (_overridden.Count == 0) {
                Plugin.Log.LogWarning("map: no arrow bindings found to suppress; game arrows will fight the cursor");
            }
        }

        public void Restore() {
            if (!_active) {
                return;
            }
            _active = false;
            foreach (var (action, index) in _overridden) {
                action.RemoveBindingOverride(index);
            }
            _overridden.Clear();
            var eventSystem = EventSystem.current;
            if (eventSystem != null) {
                eventSystem.sendNavigationEvents = true;
            }
        }

        private static bool IsStillOverridden((InputAction Action, int Index) entry)
            => entry.Index < entry.Action.bindings.Count
               && entry.Action.bindings[entry.Index].overridePath == string.Empty;

        private static bool IsArrow(string path) {
            foreach (var arrow in ArrowPaths) {
                if (path.EndsWith(arrow, System.StringComparison.OrdinalIgnoreCase)) {
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
