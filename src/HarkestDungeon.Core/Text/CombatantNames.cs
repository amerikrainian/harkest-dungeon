using System.Collections.Generic;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Text {
    /// <summary>Speakable combat identities.</summary>
    public static class CombatantNames {
        /// <summary>The name to speak for one combatant. <paramref name="teamNames"/> holds the
        /// names of every living combatant on its side, this one included: when several of them
        /// share the name (a pack of identical enemies), the combatant's rank is appended so
        /// each reads distinctly; a unique name is spoken bare.</summary>
        public static string Spoken(string name, int rank, IEnumerable<string?> teamNames) {
            int holders = 0;
            foreach (string? teamName in teamNames) {
                if (teamName == name) {
                    holders++;
                }
            }
            return holders > 1 ? S.CombatantNumbered(name, rank) : name;
        }
    }
}
