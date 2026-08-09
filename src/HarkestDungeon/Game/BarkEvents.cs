using Assets.Code.Bark;
using Assets.Code.Game;
using Assets.Code.Source;
using FMODUnity;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// Road and inn bark speech, wired as postfixes on the bark spawner - the one choke point
    /// every speech bubble outside combat passes through: banter act-outs and relationship
    /// exchanges (the hero ribbon spawns those directly, no bark event ever fires), road-event
    /// reaction and node-approach barks, pet noises, the barks the ribbon and the inn's rest
    /// slots relay from the bark event, and rest-item reactions and refusals. The patches only
    /// compose pending lines into the mode's queue (the road sense's on the road, the inn
    /// events' at the inn); speech goes out on the pump path. Combat runs the same spawner for
    /// its bubbles, so those stay with the combat module's own bark-event listener.
    /// </summary>
    public static class BarkEvents {
        private static bool _attached;

        /// <summary>The road delivery route, wired at startup to the road sense's pending
        /// queue.</summary>
        public static System.Action<string> RoadSink;

        /// <summary>Idempotent; attached at startup.</summary>
        public static void Attach() {
            if (_attached) {
                return;
            }
            _attached = true;
            var harmony = new Harmony("dd2a11y.barks");
            Patch(harmony, AccessTools.Method(typeof(BarkSpawnerSingleton), nameof(BarkSpawnerSingleton.SpawnBark),
                new[] { typeof(uint), typeof(string), typeof(BarkDisplayType), typeof(SourceType),
                        typeof(string), typeof(EventReference) }), nameof(ActorBarkShown));
            Patch(harmony, AccessTools.Method(typeof(BarkSpawnerSingleton), nameof(BarkSpawnerSingleton.SpawnBark),
                new[] { typeof(Transform), typeof(bool), typeof(string), typeof(BarkDisplayType), typeof(SourceType),
                        typeof(string), typeof(EventReference) }), nameof(WorldBarkShown));
        }

        private static void Patch(Harmony harmony, System.Reflection.MethodInfo target, string postfix) {
            if (target == null) {
                Plugin.Log.LogError($"BarkEvents: SpawnBark overload for {postfix} not found; those road barks will not speak");
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(BarkEvents), postfix)));
        }

        private static bool OnRoad => GameModeMgr.CurrentMode == GameModeType.DRIVING;
        private static bool AtInn => GameModeMgr.CurrentMode == GameModeType.INN;

        // A hero's speech bubble. The key arrives already resolved by the game's cascading
        // lookup, so a miss is a shape change worth hearing about.
        private static void ActorBarkShown(uint actorGuid, string barkKey) {
            string text = Resolve(barkKey);
            if (text == null) {
                return;
            }
            string speaker = Actors.Name(Actors.Get(actorGuid));
            Sink(speaker == null ? text : S.BarkLine(speaker, text));
        }

        // A bubble anchored to the world (the stagecoach pet's cage): no speaking actor, the
        // text alone.
        private static void WorldBarkShown(string barkKey) {
            string text = Resolve(barkKey);
            if (text != null) {
                Sink(text);
            }
        }

        private static void Sink(string line) {
            if (OnRoad) {
                RoadSink?.Invoke(line);
            } else {
                InnEvents.Enqueue(line);
            }
        }

        private static string Resolve(string barkKey) {
            if (!OnRoad && !AtInn) {
                return null;
            }
            string text = GameLoc.TryGet(barkKey);
            if (text == null) {
                Plugin.Log.LogWarning($"BarkEvents: bark key \"{barkKey}\" has no localized text");
            }
            return text;
        }
    }
}
