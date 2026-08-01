using Assets.Code.Game;
using Assets.Code.Tutorial;
using Assets.Code.UI;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Corner-toast announcements: the game's ToastManager has no display event, so its show
    /// methods carry postfixes that route a spoken line by mode - in combat into the combat
    /// pending queue, on the road into the road sense's pending queue, each spoken from the
    /// pump. Tutorial toasts speak the game's tutorial title; message toasts speak their own
    /// localized text. Objective toasts ride the model event instead (handled in CombatEvents);
    /// loot toasts ride the loot event (RoadSense).
    /// </summary>
    public static class ToastEvents {
        private static bool _attached;

        /// <summary>The road delivery route, wired at startup to the road sense's pending
        /// queue.</summary>
        public static System.Action<string> RoadSink;

        /// <summary>Idempotent; attached at startup - toasts pop on the road before any
        /// combat has resolved.</summary>
        public static void Attach() {
            if (_attached) {
                return;
            }
            _attached = true;
            var harmony = new Harmony("dd2a11y.toasts");
            PatchPostfix(harmony, nameof(ToastManager.ShowTutorialToast), nameof(TutorialShown));
            PatchPostfix(harmony, nameof(ToastManager.ShowMessageToast), nameof(MessageShown));
        }

        private static void PatchPostfix(Harmony harmony, string original, string postfix) {
            var target = AccessTools.Method(typeof(ToastManager), original);
            if (target == null) {
                Plugin.Log.LogError($"ToastEvents: ToastManager.{original} not found; that toast kind will not speak");
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(ToastEvents), postfix)));
        }

        private static bool InCombat => GameModeMgr.CurrentMode == GameModeType.COMBAT;
        private static bool OnRoad => GameModeMgr.CurrentMode == GameModeType.DRIVING;

        private static void Deliver(string line) {
            if (string.IsNullOrEmpty(line)) {
                return;
            }
            if (InCombat) {
                CombatEvents.Enqueue(line);
            } else if (OnRoad) {
                RoadSink?.Invoke(line);
            }
        }

        private static void TutorialShown(TutorialType tutorialType) {
            if (tutorialType == null) {
                return;
            }
            string title = GameLoc.TryGet($"tutorial_t{tutorialType.m_eventId}_title");
            if (title != null) {
                Deliver(S.ToastTutorial(title));
            }
        }

        private static void MessageShown(string toastLocKey) {
            Deliver(GameLoc.TryGet(toastLocKey));
        }
    }
}
