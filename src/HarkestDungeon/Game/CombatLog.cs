using System.Collections.Generic;

namespace DD2A11y.Game {
    /// <summary>
    /// The battle-event history behind the combat buffer: a capped list of already-composed
    /// spoken lines, appended as combat unfolds and reviewed on demand with the buffer keys. A
    /// history is a record, not cached state - its lines are what WAS announced-worthy, composed
    /// at the moment the event happened. Filled from the combat screen's pump path and emptied
    /// when the battle ends, so the buffer only exists in combat.
    /// </summary>
    public static class CombatLog {
        private const int Cap = 200;
        private static readonly List<string> _lines = new List<string>();

        public static void Append(string line) {
            if (string.IsNullOrWhiteSpace(line)) {
                return;
            }
            _lines.Add(line);
            if (_lines.Count > Cap) {
                _lines.RemoveAt(0);
            }
        }

        /// <summary>Empties the log; the combat buffer exists only while a battle stands.</summary>
        public static void Clear() => _lines.Clear();

        public static IEnumerable<string> Lines() => _lines;
    }
}
