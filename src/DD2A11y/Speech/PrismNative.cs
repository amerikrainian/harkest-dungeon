using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DD2A11y.Speech {
    /// <summary>
    /// Thin P/Invoke layer over prism.dll (https://github.com/ethindp/prism) - a unified native
    /// abstraction over screen readers / TTS (NVDA, JAWS, SAPI, OneCore, ...). prism.dll is deployed
    /// into the plugin folder, which is not on the loader's search path, so <see cref="Preload"/>
    /// LoadLibrary's it by absolute path first; the DllImports then resolve to the already-loaded
    /// module by name.
    /// </summary>
    internal static class PrismNative {
        private const string Dll = "prism";

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string path);

        public static bool Preload(string absolutePath) => LoadLibraryW(absolutePath) != IntPtr.Zero;

        // Must match PrismError in prism.h (third_party/prism/prism.h) for the running version.
        public enum PrismError {
            Ok = 0,
            NotInitialized,
            InvalidParam,
            NotImplemented,
            NoVoices,
            VoiceNotFound,
            SpeakFailure,
            MemoryFailure,
            RangeOutOfBounds,
            Internal,
            NotSpeaking,
            NotPaused,
            AlreadyPaused,
            InvalidUtf8,
            InvalidOperation,
            AlreadyInitialized,
            BackendNotAvailable,
            Unknown,
            InvalidAudioFormat,
            InternalBackendLimitExceeded,
            BackendEnteredUndefinedState,
        }

        [DllImport(Dll, EntryPoint = "prism_init")]
        public static extern IntPtr Init(IntPtr config);

        [DllImport(Dll, EntryPoint = "prism_shutdown")]
        public static extern void Shutdown(IntPtr ctx);

        [DllImport(Dll, EntryPoint = "prism_registry_create_best")]
        public static extern IntPtr RegistryCreateBest(IntPtr ctx);

        [DllImport(Dll, EntryPoint = "prism_backend_initialize")]
        public static extern PrismError BackendInitialize(IntPtr backend);

        [DllImport(Dll, EntryPoint = "prism_backend_free")]
        public static extern void BackendFree(IntPtr backend);

        [DllImport(Dll, EntryPoint = "prism_backend_name")]
        private static extern IntPtr BackendNameRaw(IntPtr backend);

        public static string BackendName(IntPtr backend) => Utf8FromPtr(BackendNameRaw(backend));

        // Output = route through the screen reader (respects the user's SR settings + braille).
        [DllImport(Dll, EntryPoint = "prism_backend_output")]
        private static extern PrismError BackendOutputRaw(IntPtr backend, byte[] textUtf8, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        public static PrismError BackendOutput(IntPtr backend, string text, bool interrupt)
            => BackendOutputRaw(backend, Utf8(text), interrupt);

        [DllImport(Dll, EntryPoint = "prism_backend_stop")]
        public static extern PrismError BackendStop(IntPtr backend);

        private static byte[] Utf8(string s) {
            // Native side expects null-terminated UTF-8.
            int len = Encoding.UTF8.GetByteCount(s);
            var buf = new byte[len + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buf, 0);
            return buf;
        }

        private static string Utf8FromPtr(IntPtr ptr) {
            if (ptr == IntPtr.Zero) {
                return null;
            }
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) {
                len++;
            }
            if (len == 0) {
                return string.Empty;
            }
            var bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
