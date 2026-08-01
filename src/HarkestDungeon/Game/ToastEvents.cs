using Assets.Code.Game;
using Assets.Code.Tutorial;
using Assets.Code.UI;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Announcements for eventless UI surfaces, wired as postfixes. The toast manager's show
    /// methods route a spoken line by mode - in combat into the combat pending queue, on the
    /// road into the road sense's pending queue, each spoken from the pump. Tutorial toasts
    /// speak the game's tutorial title; message toasts speak their own localized text. The
    /// coach's low-flame ambush pop rides the combat queue outright: it plays as the ambush
    /// battle spins up, so its line speaks with the battle's opening. Objective toasts ride
    /// the model event instead (handled in CombatEvents); loot toasts ride the loot event
    /// (RoadSense).
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
            PatchPostfix(harmony, typeof(ToastManager), nameof(ToastManager.ShowTutorialToast), nameof(TutorialShown));
            PatchPostfix(harmony, typeof(ToastManager), nameof(ToastManager.ShowMessageToast), nameof(MessageShown));
            PatchPostfix(harmony, typeof(StageCoachTorchUiBhv),
                nameof(StageCoachTorchUiBhv.ShowLowTorchAmbushPopText), nameof(AmbushPopShown));
        }

        private static void PatchPostfix(Harmony harmony, System.Type type, string original, string postfix) {
            var target = AccessTools.Method(type, original);
            if (target == null) {
                Plugin.Log.LogError($"ToastEvents: {type.Name}.{original} not found; that surface will not speak");
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

        private static void AmbushPopShown() {
            string line = GameLoc.TryGet("driving_torch_ambush_label");
            if (line != null) {
                CombatEvents.Enqueue(line);
            }
        }
    }
}
