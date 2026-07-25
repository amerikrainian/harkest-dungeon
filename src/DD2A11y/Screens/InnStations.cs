using TMPro;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>Shared reads for the inn's station sub-screens.</summary>
    internal static class InnStations {
        /// <summary>The inn header's title text, which the game retitles per open station
        /// ("The Provisioner", "Mastery Trainer"); null when absent.</summary>
        internal static string Title() {
            foreach (var tmp in Object.FindObjectsOfType<TMP_Text>()) {
                if (tmp.gameObject.name == "inn_title" && !string.IsNullOrWhiteSpace(tmp.text)) {
                    return tmp.text;
                }
            }
            return null;
        }
    }
}
