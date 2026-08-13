using Assets.Code.Affinity.Events;
using Assets.Code.Events;
using Assets.Code.Game;
using DD2A11y.Core.Speech;

namespace DD2A11y.Game {
    /// <summary>
    /// Affinity leaning changes outside combat ("Dismas and Audrey, affinity +1"): a story
    /// choice's alignment fallout, an inn exchange - the game shows them only as the leaning
    /// hearts over the ribbons plus a story's outcome sting, through its non-combat leaning
    /// event. Combat speaks its own tick event, so this listener stands down there. Lines
    /// route to the active surface's transient queue.
    /// </summary>
    public static class AffinityEvents {
        /// <summary>Where road-mode lines go (RoadSense's pending queue), wired at load.</summary>
        public static System.Action<string> RoadSink;

        public static void Attach() {
            EventManager.AddListener<EventAffinityConnectionLeaningChange>(HandleLeaningChange);
        }

        private static void HandleLeaningChange(EventAffinityConnectionLeaningChange evt) {
            if (GameModeMgr.CurrentMode == GameModeType.COMBAT) {
                return; // the combat tick handler speaks these
            }
            string line = Targeting.AffinityLine(evt.m_Connection, evt.m_LeaningChange);
            if (line == null) {
                return;
            }
            if (GameModeMgr.CurrentMode == GameModeType.DRIVING) {
                RoadSink?.Invoke(line);
            } else if (GameModeMgr.CurrentMode == GameModeType.INN) {
                InnEvents.Enqueue(line);
            } else {
                SpeechPipeline.Instance?.Speak(line, interrupt: false);
            }
        }
    }
}
