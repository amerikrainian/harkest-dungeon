using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Item;
using Assets.Code.Tutorial;
using Assets.Code.UI;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Announcements for eventless UI surfaces, wired as postfixes. The toast manager's show
    /// methods route a spoken line by mode - in combat into the combat pending queue, on the
    /// road into the road sense's pending queue, at the inn into the inn's, and straight to
    /// queued speech everywhere else (the game shows toasts in every mode via its backup
    /// container - the flame-unlock toast fires on the game-over screen, tutorials at the
    /// inn). Tutorial toasts speak the game's tutorial title; message toasts speak their own
    /// localized text; objective toasts speak the hero and the reward items the icons show -
    /// the toast is the one surface both goal-completion paths (the bare event and the
    /// loot-manager reward path) funnel through. The coach's low-flame ambush pop rides the
    /// combat queue outright: it plays as the ambush battle spins up, so its line speaks
    /// with the battle's opening. Loot toasts ride the loot event (RoadSense).
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
            PatchPostfix(harmony, typeof(ToastManager), nameof(ToastManager.ShowHeroObjectiveCompleteToast),
                nameof(ObjectiveShown));
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
            } else if (GameModeMgr.CurrentMode == GameModeType.INN) {
                InnEvents.Enqueue(line);
            } else {
                SpeechPipeline.Instance?.Speak(line, interrupt: false);
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

        // The toast identifies the goal by the hero's portrait and shows the reward items as
        // icons; the line mirrors that - the hero's name, then each reward's own title.
        private static void ObjectiveShown(uint actorGuid, List<ItemInstance> itemRewards) {
            string name = Actors.SpokenName(Actors.Get(actorGuid));
            if (name == null) {
                return;
            }
            string line = S.ToastObjective(name);
            if (itemRewards != null) {
                foreach (var item in itemRewards) {
                    line = SpokenLine.Join(line,
                        ItemDescription.GetTitle(item.GetItemDefinition(), item.GetQty()));
                }
            }
            Deliver(line);
        }

        private static void AmbushPopShown() {
            string line = GameLoc.TryGet("driving_torch_ambush_label");
            if (line != null) {
                CombatEvents.Enqueue(line);
            }
        }
    }
}
