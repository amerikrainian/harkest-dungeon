using System.Collections.Generic;
using Assets.Code.Subtitles;
using HarmonyLib;

namespace DD2A11y.Game {
    /// <summary>
    /// The game's on-screen subtitle text, spoken as each line appears and kept as the
    /// subtitles buffer's history. Every subtitle surface - the general manager (in-run
    /// narration) and the cinematic manager (video cutscenes) - funnels its display changes
    /// through <see cref="SubtitlesUtils.TryUpdateDisplay"/>; the postfix records the
    /// localized line whenever one becomes active under the game's own visibility gate (the
    /// Subtitles toggle plus its dev-pref overrides), so the mod speaks exactly what a
    /// sighted player sees appear. Lines queue here and speak from the pump; the history
    /// answers the buffer only while the gate holds, so the buffer exists only with
    /// subtitles on.
    /// </summary>
    public static class SubtitleEvents {
        private const int Cap = 200;
        private static readonly List<string> _pending = new List<string>();
        private static readonly List<string> _log = new List<string>();
        private static readonly List<string> _none = new List<string>();
        private static bool _attached;

        public static void Attach() {
            if (_attached) {
                return;
            }
            _attached = true;
            var harmony = new Harmony("dd2a11y.subtitles");
            var target = AccessTools.Method(typeof(SubtitlesUtils), nameof(SubtitlesUtils.TryUpdateDisplay));
            if (target == null) {
                Plugin.Log.LogError("SubtitleEvents: SubtitlesUtils.TryUpdateDisplay not found; subtitles will not speak");
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(
                AccessTools.Method(typeof(SubtitleEvents), nameof(DisplayUpdated))));
        }

        // A subtitle display change; an active one carries the line's loc key.
        private static void DisplayUpdated(bool isBecomingActive, string locKey) {
            if (!isBecomingActive || string.IsNullOrEmpty(locKey)
                || !SubtitlesUtils.ShouldSubtitleBeVisible()) {
                return;
            }
            string line = GameLoc.TryGet(locKey);
            if (line == null) {
                Plugin.Log.LogWarning("subtitles: no string for loc key " + locKey);
                return;
            }
            _pending.Add(line);
            _log.Add(line);
            if (_log.Count > Cap) {
                _log.RemoveAt(0);
            }
        }

        public static IReadOnlyList<string> Drain() {
            if (_pending.Count == 0) {
                return null;
            }
            var drained = new List<string>(_pending);
            _pending.Clear();
            return drained;
        }

        /// <summary>The subtitles buffer's source: the shown-line history while the game's
        /// subtitles setting is on, empty (hiding the buffer) while it is off.</summary>
        public static IEnumerable<string> Lines()
            => SubtitlesUtils.ShouldSubtitleBeVisible() ? _log : _none;
    }
}
