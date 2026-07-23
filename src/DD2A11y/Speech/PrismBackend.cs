using System;
using DD2A11y.Core.Speech;

namespace DD2A11y.Speech {
    /// <summary>
    /// ISpeechBackend over Prism. Owns the native context + chosen backend lifecycle. Policy lives
    /// in the Core SpeechPipeline (clean / interrupt); this only emits.
    /// </summary>
    public sealed class PrismBackend : ISpeechBackend {
        private IntPtr _ctx;
        private IntPtr _backend;

        public bool IsAvailable { get; private set; }

        public bool Initialize(string dllPath) {
            try {
                if (!PrismNative.Preload(dllPath)) {
                    Plugin.Log.LogError("Prism: LoadLibrary failed for " + dllPath);
                    return false;
                }
                _ctx = PrismNative.Init(IntPtr.Zero);
                if (_ctx == IntPtr.Zero) {
                    Plugin.Log.LogError("Prism: prism_init returned null context");
                    return false;
                }

                _backend = PrismNative.RegistryCreateBest(_ctx);
                if (_backend == IntPtr.Zero) {
                    Plugin.Log.LogError("Prism: no usable speech backend (is a screen reader running?)");
                    return false;
                }

                // create_best already returns an initialized backend; calling initialize again is
                // harmless and reports AlreadyInitialized, which we treat as success.
                var err = PrismNative.BackendInitialize(_backend);
                if (err != PrismNative.PrismError.Ok && err != PrismNative.PrismError.AlreadyInitialized) {
                    Plugin.Log.LogError("Prism: backend initialize failed: " + err);
                    return false;
                }

                IsAvailable = true;
                Plugin.Log.LogInfo("Prism backend ready: " + PrismNative.BackendName(_backend));
                return true;
            } catch (DllNotFoundException) {
                Plugin.Log.LogError("Prism: prism.dll not found at " + dllPath);
                return false;
            } catch (Exception ex) {
                Plugin.Log.LogError("Prism: initialization error: " + ex);
                return false;
            }
        }

        public void Speak(string text, bool interrupt) {
            if (!IsAvailable) {
                return;
            }
            try {
                var err = PrismNative.BackendOutput(_backend, text, interrupt);
                if (err != PrismNative.PrismError.Ok) {
                    Plugin.Log.LogWarning($"Prism: output returned {err}, line not spoken: {text}");
                }
            } catch (Exception ex) {
                Plugin.Log.LogWarning("Prism: speak failed: " + ex.Message);
            }
        }

        public void Stop() {
            if (!IsAvailable) {
                return;
            }
            try {
                PrismNative.BackendStop(_backend);
            } catch (Exception ex) {
                Plugin.Log.LogWarning("Prism: stop failed: " + ex.Message);
            }
        }

        public void Shutdown() {
            if (_backend != IntPtr.Zero) {
                PrismNative.BackendFree(_backend);
                _backend = IntPtr.Zero;
            }
            if (_ctx != IntPtr.Zero) {
                PrismNative.Shutdown(_ctx);
                _ctx = IntPtr.Zero;
            }
            IsAvailable = false;
        }
    }
}
