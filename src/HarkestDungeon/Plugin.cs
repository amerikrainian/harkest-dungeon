using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace DD2A11y {
    [BepInPlugin("harkestdungeon", "Harkest Dungeon", Version)]
    public sealed class Plugin : BaseUnityPlugin {
        public const string Version = "0.4.0";

        internal static ManualLogSource Log;
        internal static Runtime Runtime;

        private void Awake() {
            Log = Logger;
            try {
                Runtime = new Runtime(Path.GetDirectoryName(Info.Location), Version, Config);
            } catch (Exception ex) {
                Logger.LogError("Harkest Dungeon failed to initialize: " + ex);
                return;
            }

            var pump = new GameObject("DD2A11y") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(pump);
            pump.AddComponent<Pump>();
            Application.quitting += () => {
                Runtime?.Dispose();
                Runtime = null;
            };
            Logger.LogInfo("Harkest Dungeon " + Version + " loaded.");
        }

        private void OnDestroy() {
            // The game sweeps the BepInEx manager object during boot; the mod lives on its own
            // hidden GameObject, so this component's death is expected and harmless.
            Log.LogInfo("plugin component destroyed (runtime lives on the pump object)");
        }
    }

    /// <summary>The single per-frame driver: everything the mod does each frame runs from here.</summary>
    public sealed class Pump : MonoBehaviour {
        private bool _announced;

        private void Update() {
            if (!_announced) {
                _announced = true;
                Plugin.Log.LogInfo("pump ticking");
            }
            Plugin.Runtime?.Tick();
        }

        private void OnDestroy() {
            Plugin.Log.LogWarning("pump destroyed");
        }
    }
}
