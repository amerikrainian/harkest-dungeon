using System;
using System.Collections.Generic;
using DD2A11y.Core.Text;

namespace DD2A11y.Core.Speech {
    /// <summary>
    /// The single funnel for everything the mod says. Owns policy: clean the text (strip TMP markup)
    /// and route to the backend. Callers never touch the backend directly. House rule: navigation
    /// interrupts, ambient announcements queue.
    /// </summary>
    public sealed class SpeechPipeline {
        /// <summary>Set once by the plugin at load; null in unit tests that construct their own.</summary>
        public static SpeechPipeline? Instance { get; set; }

        /// <summary>
        /// Optional tap invoked with (text, interrupt, source) for every line that clears the clean
        /// gate, so it sees exactly what was voiced. The dev server sets this to read spoken text back
        /// (it can't hear the TTS). Source is the speaking class's name (the first stack frame outside
        /// this pipeline); it is computed only while the tap is attached. Null in normal play and in
        /// unit tests.
        /// </summary>
        public static Action<string, bool, string?>? Spoken;

        private readonly ISpeechBackend _backend;

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When set, the backend is not driven but the <see cref="Spoken"/> tap still fires, so a
        /// headless dev run reads back through /speech without depending on a screen reader. Set by
        /// the plugin from DD2A11Y_NO_SPEECH. Distinct from <see cref="Enabled"/>, which gates the
        /// line entirely (tap included).
        /// </summary>
        public bool Muted { get; set; }

        public SpeechPipeline(ISpeechBackend backend) {
            _backend = backend;
        }

        public void Speak(string? text, bool interrupt = false) {
            if (!Enabled) {
                return;
            }

            string clean = TextFilter.Clean(text);
            if (clean.Length == 0) {
                return;
            }

            if (Holding) {
                // Held: the first line cuts off what was being said when the hold opened (the
                // help row the player pressed Enter on); everything after it queues behind it,
                // in order. All of it is remembered.
                interrupt = _held.Count == 0;
                _held.Add(clean);
            }

            if (!Muted) {
                _backend.Speak(clean, interrupt);
            }
            Spoken?.Invoke(clean, interrupt, Spoken != null ? CallerName() : null);
        }

        // The speaking class, for the dev tap's attribution: the first stack frame whose type is not
        // this pipeline, with compiler-generated closure types resolved to the class that declared
        // them. Dev-only cost: computed only while the tap is attached, and speech is low-frequency.
        private static string? CallerName() {
            var trace = new System.Diagnostics.StackTrace(2, false);
            for (int i = 0; i < trace.FrameCount; i++) {
                Type? type = trace.GetFrame(i)?.GetMethod()?.DeclaringType;
                while (type != null && type.Name.StartsWith("<", StringComparison.Ordinal)) {
                    type = type.DeclaringType; // a closure/state-machine; name its declaring class
                }
                if (type == null || type == typeof(SpeechPipeline)) {
                    continue;
                }
                return type.Name;
            }
            return null;
        }

        public void Stop() {
            _backend.Stop();
        }

        // ---- the hold ----

        // A hold nothing ends (its closer died with a screen) lapses by itself, in seconds.
        private const double HoldLapse = 3.0;
        private readonly List<string> _held = new List<string>();
        private bool _holding;
        private double _holdUntil;

        /// <summary>The clock a hold lapses by, in seconds. Set by the plugin; null in unit tests
        /// that construct their own pipeline, where a hold lasts until ended.</summary>
        public Func<double>? Clock { get; set; }

        /// <summary>While a hold is open, its first line interrupts and nothing after it does:
        /// every later line queues, in the order it was said. The key help opens one to run a key
        /// for the player: whatever the key says (its feedback interrupts, as a key press wants,
        /// some of it frames later), then the focus again, heard in that order instead of each
        /// cutting the last off. Ended by <see cref="EndHold"/>, by the next real key press, or
        /// by itself after a few seconds.</summary>
        public void BeginHold() {
            _held.Clear();
            _holding = true;
            _holdUntil = Clock != null ? Clock() + HoldLapse : double.MaxValue;
        }

        public void EndHold() => _holding = false;

        public bool Holding {
            get {
                if (_holding && Clock != null && Clock() > _holdUntil) {
                    _holding = false;
                }
                return _holding;
            }
        }

        /// <summary>What was said since the hold opened, cleaned as spoken.</summary>
        public IReadOnlyList<string> Held => _held;
    }
}
