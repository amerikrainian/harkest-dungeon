using System.Collections.Generic;

namespace DD2A11y.Game {
    /// <summary>The game's per-class blurb shown on the hero select panel: the verbose
    /// description ("A resolute defender...") and the descriptor list ("+ Front Rank + Guard
    /// ..."), from the same loc keys the game binds to that panel.</summary>
    public static class ClassDescription {
        public static IEnumerable<string> Lines(string classId) {
            if (string.IsNullOrEmpty(classId)) {
                yield break;
            }
            string description = GameLoc.TryGet("actor_verbose_description_" + classId);
            if (description != null) {
                yield return description;
            }
            string descriptors = GameLoc.TryGet("actor_descriptors_" + classId);
            if (descriptors != null) {
                yield return descriptors;
            }
        }
    }
}
