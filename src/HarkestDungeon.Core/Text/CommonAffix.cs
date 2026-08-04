using System.Collections.Generic;

namespace DD2A11y.Core.Text {
    /// <summary>
    /// Strips the word a set of related labels all share ("STUN RESIST, MOVE RESIST" reads as
    /// "STUN, MOVE") so a joined readout is not the shared word over and over. The affix is
    /// found character-level - unspaced languages work, and a language that leads with the
    /// shared word ("RESISTANCE AU...") strips from the front - taking whichever of the common
    /// prefix or suffix is longer, when it spans at least two characters and leaves every
    /// label non-empty; otherwise the labels come back untouched.
    /// </summary>
    public static class CommonAffix {
        public static List<string> Shorten(IReadOnlyList<string> labels) {
            var result = new List<string>(labels.Count);
            for (int i = 0; i < labels.Count; i++) {
                result.Add(labels[i]);
            }
            if (labels.Count < 2) {
                return result;
            }
            int prefix = CommonPrefixLength(labels);
            int suffix = CommonSuffixLength(labels);
            bool fromFront = prefix >= suffix;
            int strip = fromFront ? prefix : suffix;
            if (strip < 2) {
                return result;
            }
            var shortened = new List<string>(labels.Count);
            foreach (string label in labels) {
                string cut = (fromFront ? label.Substring(strip) : label.Substring(0, label.Length - strip)).Trim();
                if (cut.Length == 0) {
                    return result; // a label was nothing but the affix - keep them all whole
                }
                shortened.Add(cut);
            }
            return shortened;
        }

        private static int CommonPrefixLength(IReadOnlyList<string> labels) {
            int length = labels[0].Length;
            for (int i = 1; i < labels.Count; i++) {
                string label = labels[i];
                int shared = 0;
                while (shared < length && shared < label.Length && label[shared] == labels[0][shared]) {
                    shared++;
                }
                length = shared;
            }
            return length;
        }

        private static int CommonSuffixLength(IReadOnlyList<string> labels) {
            int length = labels[0].Length;
            for (int i = 1; i < labels.Count; i++) {
                string label = labels[i];
                int shared = 0;
                while (shared < length && shared < label.Length
                       && label[label.Length - 1 - shared] == labels[0][labels[0].Length - 1 - shared]) {
                    shared++;
                }
                length = shared;
            }
            return length;
        }
    }
}
