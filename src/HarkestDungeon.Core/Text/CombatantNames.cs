using System.Collections.Generic;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Text {
    /// <summary>Speakable combat identities: when several living teammates share a name (a
    /// pack of identical enemies), each carries a stable ordinal in first-sight order -
    /// battlefield position order at battle start - so a position shuffle never renames
    /// anyone. Ordinals count only teammates still standing: a death compacts the survivors
    /// down (Widow 2 becomes Widow 1) and a sole survivor drops the number. A unique name is
    /// spoken bare.</summary>
    public sealed class CombatantNames {
        private readonly Dictionary<uint, long> _seen = new Dictionary<uint, long>();
        private long _next;

        /// <summary>Record first sight of a side's members, in battlefield position order; an
        /// already-seen member keeps its place.</summary>
        public void Observe(IEnumerable<uint> teamByPosition) {
            foreach (uint guid in teamByPosition) {
                if (!_seen.ContainsKey(guid)) {
                    _seen[guid] = _next++;
                }
            }
        }

        /// <summary>The name to speak for one combatant among its side's living (guid, name)
        /// pairs, this one included.</summary>
        public string? Spoken(uint guid, string? name, IEnumerable<KeyValuePair<uint, string?>> team) {
            if (name == null) {
                return null;
            }
            long sequence = SequenceOf(guid);
            int holders = 0;
            int ordinal = 1;
            foreach (var member in team) {
                if (member.Value != name) {
                    continue;
                }
                holders++;
                if (member.Key != guid && SequenceOf(member.Key) < sequence) {
                    ordinal++;
                }
            }
            return holders > 1 ? S.CombatantNumbered(name, ordinal) : name;
        }

        public void Reset() {
            _seen.Clear();
            _next = 0;
        }

        // An unrecorded combatant sorts after every recorded teammate.
        private long SequenceOf(uint guid)
            => _seen.TryGetValue(guid, out long sequence) ? sequence : long.MaxValue;
    }
}
