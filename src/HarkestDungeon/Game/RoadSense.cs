using System;
using System.Collections.Generic;
using Assets.Code.Actor.Events;
using Assets.Code.Bark.Events;
using Assets.Code.Events;
using Assets.Code.Game;
using Assets.Code.Item;
using Assets.Code.Item.Events;
using Assets.Code.Map;
using Assets.Code.Map.Events;
using Assets.Code.Map.Generation;
using Assets.Code.Map.Generation.Row;
using Assets.Code.Map.RoadEvents;
using Assets.Code.Run;
using Assets.Code.Run.Events;
using Assets.Code.UI.Events;
using Assets.Code.Utils;
using DD2A11y.Core.Audio;
using DD2A11y.Input;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// The free-driving soundscape, ticked from the pump while the DRIVING mode runs uncaptured
    /// (the game keeps the keyboard for steering; cues carry what held keys would make speech
    /// miss). EVERY uncollected roadside pickup in hearing range sounds as its own continuous
    /// loop, and every map node in range as a loop of its destination's identity timbre (with a
    /// louder one-shot announcing its first appearance) - each re-aimed every frame, pan to its
    /// bearing and louder as it nears, so the whole nearby layout stays audible and steering
    /// reflects immediately. Candidate discovery (the allocating scene sweep) stays on a slow
    /// clock; the per-frame work runs over the cached candidates allocation-free, and a loop
    /// cuts the frame its object is collected, executed, or out of range. Collection and damage
    /// arrive as cue plus queued speech; the coach's stop/start, an approaching fork, the
    /// road's edge, an event zone's enter/exit (with the interact prompt on opt-in events), an
    /// ambush, a danger stretch, and a Loathing advance each get their own cue. Event listeners
    /// only compose pending lines; every sound and word goes out on the pump path.
    /// </summary>
    public sealed class RoadSense {
        private static readonly AccessTools.FieldRef<TriggerIntersectionBhv, IntersectionState> StateField =
            AccessTools.FieldRefAccess<TriggerIntersectionBhv, IntersectionState>("m_CurrentState");
        private static readonly AccessTools.FieldRef<RoadEventBhv, RoadEventInteractionType> InteractionTypeField =
            AccessTools.FieldRefAccess<RoadEventBhv, RoadEventInteractionType>("m_interactionType");
        private static readonly AccessTools.FieldRef<TileNodeBhv, TriggerState> NodeStateField =
            AccessTools.FieldRefAccess<TileNodeBhv, TriggerState>("m_triggerState");

        // The sensing radius is the player's setting, read live per scan. Nodes reach half
        // again as far as pickups (their authored 120 against the pickups' 80); the one
        // setting scales both, keeping that ratio.
        private readonly Core.Settings.IntSetting _sensingRange;
        private const float NodeRangeFactor = 1.5f;
        private const float ScanInterval = 0.7f;
        // Off-center distance as a fraction of the road's half-width: the bump sounds past
        // Warn and arms again under Rearm, so riding the edge does not chatter.
        private const float EdgeWarn = 0.85f;
        private const float EdgeRearm = 0.7f;

        private readonly IAudioEngine _audio;
        private readonly Action<string, bool> _speak;
        private readonly InputGate _gate;
        // Cue and/or line to deliver on the next pump tick; a null cue is speech-only (the
        // game's own sfx already marks the moment).
        private readonly List<KeyValuePair<AudioCue?, string>> _pending = new List<KeyValuePair<AudioCue?, string>>();
        private readonly HashSet<int> _announcedForks = new HashSet<int>();
        private readonly HashSet<int> _announcedNodes = new HashSet<int>();
        private float _nextScanTime;
        private RoadEventBhv[] _pickupCandidates = new RoadEventBhv[0];
        private TileNodeBhv[] _nodeCandidates = new TileNodeBhv[0];
        private readonly Dictionary<int, IAudioLoop> _pickupLoops = new Dictionary<int, IAudioLoop>();
        private readonly Dictionary<int, IAudioLoop> _nodeLoops = new Dictionary<int, IAudioLoop>();
        private readonly HashSet<int> _staleLoops = new HashSet<int>();
        private bool _edgeArmed = true;
        private bool _inDanger;
        private bool _dangerKnown;

        public RoadSense(IAudioEngine audio, Action<string, bool> speak, InputGate gate,
                         Core.Settings.IntSetting sensingRange) {
            _audio = audio;
            _speak = speak;
            _gate = gate;
            _sensingRange = sensingRange;
            EventManager.AddListener<EventLootToastPresented>(HandleLootToast);
            EventManager.AddListener<EventActorHealthDamage>(HandleDamage);
            EventManager.AddListener<EventRoadEventEnter>(HandleZoneEnter);
            EventManager.AddListener<EventRoadEventExit>(HandleZoneExit);
            EventManager.AddListener<EventExecuteRoadEventStarted>(HandleRoadEventStarted);
            EventManager.AddListener<EventRunValueChanged>(HandleRunValue);
            EventManager.AddListener<EventBark>(HandleBark);
            EventManager.AddListener<EventRunResist>(HandleRunResist);
        }

        private bool OnRoad => GameModeMgr.CurrentMode == GameModeType.DRIVING
            && !Singleton<GameModeMgr>.Instance.IsChangingState();

        /// <summary>Queue a spoken line for the next road tick - the delivery route the toast
        /// patches use on the road.</summary>
        public void Post(string line) {
            _pending.Add(new KeyValuePair<AudioCue?, string>(null, line));
        }

        // A pickup rolled over: its title speaks (the game's own item name; the game's own
        // pickup sfx already marks the moment, so no mod cue). A road grant surfaces ONLY as
        // the corner loot toast - road pickups never raise the inventory widgets' loot event -
        // so the toast presenting is the hook.
        private void HandleLootToast(EventLootToastPresented evt) {
            if (!OnRoad || evt.m_item == null) {
                return;
            }
            _pending.Add(new KeyValuePair<AudioCue?, string>(
                null, ItemDescription.GetTitle(evt.m_item.GetItemDefinition(), evt.m_item.GetQty())));
        }

        // Road hazards (spikes, corpses) damage heroes outside combat; same wording as battle.
        private void HandleDamage(EventActorHealthDamage evt) {
            if (!OnRoad) {
                return;
            }
            string name = Actors.Name(Actors.Get(evt.m_ActorGuid));
            int damage = (int)evt.m_HealthDamage;
            if (name == null || damage <= 0) {
                return;
            }
            _pending.Add(new KeyValuePair<AudioCue?, string>(
                AudioCue.RoadPenalty, damage == 1 ? S.CombatTookDamageOne(name) : S.CombatTookDamage(name, damage)));
        }

        // Crossing into an event's zone: an opt-in event fires only on the game's Interact key,
        // so its prompt says so; a contact event gets the plain zone blip.
        private void HandleZoneEnter(EventRoadEventEnter evt) {
            if (!OnRoad || evt.m_roadEvent == null || evt.m_roadEvent.GetTriggerState() != TriggerState.None
                || evt.m_category != RoadEventCategory.OBJECTS) {
                return;
            }
            if (InteractionTypeField(evt.m_roadEvent) == RoadEventInteractionType.OPT_IN) {
                _pending.Add(new KeyValuePair<AudioCue?, string>(AudioCue.RoadPrompt, S.RoadInteract));
            } else {
                _pending.Add(new KeyValuePair<AudioCue?, string>(AudioCue.RoadZoneEnter, null));
            }
        }

        // Leaving a zone with the event still untriggered: a pickup passed uncollected.
        private void HandleZoneExit(EventRoadEventExit evt) {
            if (!OnRoad || evt.m_roadEvent == null || evt.m_roadEvent.GetTriggerState() != TriggerState.None
                || evt.m_roadEvent.RoadEventCategory != RoadEventCategory.OBJECTS) {
                return;
            }
            _pending.Add(new KeyValuePair<AudioCue?, string>(AudioCue.RoadZoneExit, null));
        }

        private void HandleRoadEventStarted(EventExecuteRoadEventStarted evt) {
            if (!OnRoad || evt.m_RoadEventCategory != RoadEventCategory.AMBUSH) {
                return;
            }
            _pending.Add(new KeyValuePair<AudioCue?, string>(AudioCue.RoadAmbush, null));
        }

        // A hero's speech bubble while driving (banter, act-outs); same wording as battle -
        // the key is already resolved to the specific line by the game's bark selection.
        private void HandleBark(EventBark evt) {
            if (!OnRoad) {
                return;
            }
            string speaker = Actors.Name(Actors.Get(evt.m_ActorGuid));
            string text = GameLoc.TryGet(evt.m_BarkKey);
            if (text == null) {
                Plugin.Log.LogWarning($"RoadSense: bark key \"{evt.m_BarkKey}\" has no localized text");
                return;
            }
            _pending.Add(new KeyValuePair<AudioCue?, string>(
                null, speaker == null ? text : S.BarkLine(speaker, text)));
        }

        // A Loathing advance resisted: the same pop text the coach shows, the game's own
        // format over the live resistance stat.
        private void HandleRunResist(EventRunResist evt) {
            if (!OnRoad || evt.m_ResistId != "doom") {
                return;
            }
            string format = GameLoc.TryGet("driving_doom_resist_label");
            if (format == null) {
                return;
            }
            int percent = Assets.Code.Math.MathUtils.RoundToInt(Singleton<GameTypeMgr>.Instance
                .RunDataManager.GetStatValue(RunStatType.RESISTANCE, "doom") * 100f);
            _pending.Add(new KeyValuePair<AudioCue?, string>(null, string.Format(format, percent)));
        }

        // The Loathing meter (DOOM internally) advanced.
        private void HandleRunValue(EventRunValueChanged evt) {
            if (!OnRoad || evt.m_RunValueType != RunValueType.DOOM || evt.m_IsReset
                || evt.m_CurrentValue <= evt.m_PreviousValue) {
                return;
            }
            _pending.Add(new KeyValuePair<AudioCue?, string>(AudioCue.RoadLoathing, null));
        }

        public void Tick() {
            if (!OnRoad) {
                _pending.Clear();
                _announcedForks.Clear();
                _announcedNodes.Clear();
                _dangerKnown = false;
                _pickupCandidates = new RoadEventBhv[0];
                _nodeCandidates = new TileNodeBhv[0];
                StopAllLoops();
                return;
            }

            foreach (var entry in _pending) {
                if (entry.Key != null) {
                    _audio.PlayCue(entry.Key.Value, 1f, 0f);
                }
                if (!string.IsNullOrEmpty(entry.Value)) {
                    _speak(entry.Value, false);
                }
            }
            _pending.Clear();

            // The fork menu (or any captured screen) owns the moment; the ambient layer stays quiet.
            if (_gate.Captured) {
                StopAllLoops();
                return;
            }

            var vehicle = SingletonMonoBehaviour<MapMgrBhv>.HasInstance()
                ? SingletonMonoBehaviour<MapMgrBhv>.Instance.GetVehicleControl() : null;
            if (vehicle == null) {
                StopAllLoops();
                return;
            }
            // The continuous layers re-aim every frame over the cached candidates, so steering
            // lands in the ears the same frame it lands on the road.
            UpdatePickupLoops(vehicle.transform);
            UpdateNodeLoops(vehicle.transform);
            CheckRoadEdge(vehicle.transform);
            CheckDanger();

            if (Time.unscaledTime < _nextScanTime) {
                return;
            }
            _nextScanTime = Time.unscaledTime + ScanInterval;
            // The allocating sweeps: new objects stream in with the road tiles, so a slow
            // refresh is enough; everything per-frame reads these arrays.
            _pickupCandidates = UnityEngine.Object.FindObjectsOfType<RoadEventBhv>();
            _nodeCandidates = UnityEngine.Object.FindObjectsOfType<TileNodeBhv>();
            AnnounceApproachingFork();
        }

        // One loop per uncollected pickup in range, each re-aimed every frame; the volume
        // dropoff separates them by distance. A loop past the exit margin (10% beyond the
        // range, so the boundary never flaps) or over a collected pickup goes stale and stops
        // the same frame.
        private void UpdatePickupLoops(Transform coach) {
            _staleLoops.Clear();
            foreach (var pair in _pickupLoops) {
                _staleLoops.Add(pair.Key);
            }
            foreach (var pickup in _pickupCandidates) {
                if (pickup == null || pickup.RoadEventCategory != RoadEventCategory.OBJECTS
                    || pickup.GetTriggerState() != TriggerState.None) {
                    continue;
                }
                int id = pickup.GetInstanceID();
                float range = _sensingRange.Value;
                float distance = Vector3.Distance(coach.position, pickup.transform.position);
                if (distance > (_pickupLoops.ContainsKey(id) ? range * 1.1f : range)) {
                    continue;
                }
                float pan = PanTo(coach, pickup.transform.position);
                float volume = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(distance / range));
                if (_pickupLoops.TryGetValue(id, out var loop)) {
                    loop.Update(volume, pan);
                    _staleLoops.Remove(id);
                } else {
                    _pickupLoops[id] = _audio.StartLoop(AudioCue.RoadPickup, volume, pan, PickupPitch(id));
                }
            }
            foreach (int id in _staleLoops) {
                _pickupLoops[id].Stop();
                _pickupLoops.Remove(id);
            }
        }

        // Overlapping pickups share one clip and would blend into a single ping; a per-pickup
        // pitch spreads simultaneous pings within about a semitone either way. Hashed from the
        // instance id (Unity hands them out near-sequentially) so a pickup keeps its voice
        // across range exits and re-entries.
        private static float PickupPitch(int id) {
            uint hash = (uint)id * 2654435761u;
            return 0.94f + hash % 256u / 255f * 0.12f;
        }

        // Every node in range loops its destination's identity timbre until the coach passes
        // it (executes it or leaves it behind); its first appearance also announces once,
        // louder, so a new destination registers even over the ambient layer.
        private void UpdateNodeLoops(Transform coach) {
            _staleLoops.Clear();
            foreach (var pair in _nodeLoops) {
                _staleLoops.Add(pair.Key);
            }
            foreach (var node in _nodeCandidates) {
                if (node == null || NodeStateField(node) != TriggerState.None) {
                    continue;
                }
                int id = node.GetInstanceID();
                float range = _sensingRange.Value * NodeRangeFactor;
                float distance = Vector3.Distance(coach.position, node.transform.position);
                if (distance > (_nodeLoops.ContainsKey(id) ? range * 1.1f : range)) {
                    continue;
                }
                float pan = PanTo(coach, node.transform.position);
                float volume = Mathf.Lerp(0.8f, 0.15f, Mathf.Clamp01(distance / range));
                if (_nodeLoops.TryGetValue(id, out var loop)) {
                    loop.Update(volume, pan);
                    _staleLoops.Remove(id);
                } else {
                    var cue = NodeCues.For(node.GetNodeType());
                    if (_announcedNodes.Add(id)) {
                        _audio.PlayCue(cue, 1f, pan);
                    }
                    _nodeLoops[id] = _audio.StartLoop(cue, volume, pan);
                }
            }
            foreach (int id in _staleLoops) {
                _nodeLoops[id].Stop();
                _nodeLoops.Remove(id);
            }
        }

        private void StopAllLoops() {
            foreach (var pair in _pickupLoops) {
                pair.Value.Stop();
            }
            _pickupLoops.Clear();
            foreach (var pair in _nodeLoops) {
                pair.Value.Stop();
            }
            _nodeLoops.Clear();
        }

        private static float PanTo(Transform coach, Vector3 target) {
            Vector3 to = (target - coach.position).normalized;
            float angle = Mathf.Atan2(Vector3.Dot(to, coach.right), Vector3.Dot(to, coach.forward));
            return Mathf.Clamp(Mathf.Sin(angle) * 1.4f, -1f, 1f);
        }

        // Distance from the road's centerline against its half-width, from the same public road
        // geometry the game steers the horses back with; the bump pans to the drifting side.
        private void CheckRoadEdge(Transform coach) {
            if (!MapUtils.TryGetNearOutgoingPaths(coach.position, out var paths)) {
                return;
            }
            float bestSqr = float.MaxValue;
            Vector3 bestPoint = Vector3.zero;
            foreach (var path in paths) {
                for (int i = 0; i < path.RoadListCount; i++) {
                    float t = 0f;
                    Vector3 point = path[i].GetClosestPoint(coach.position, ref t);
                    float sqr = (point - coach.position).sqrMagnitude;
                    if (sqr < bestSqr) {
                        bestSqr = sqr;
                        bestPoint = point;
                    }
                }
            }
            if (bestSqr == float.MaxValue) {
                return;
            }
            float offCenter = Mathf.Sqrt(bestSqr) / (GameConstants.ROAD_SIZE * 0.5f);
            if (_edgeArmed && offCenter >= EdgeWarn) {
                _edgeArmed = false;
                Vector3 away = (coach.position - bestPoint).normalized;
                float pan = Mathf.Clamp(Vector3.Dot(away, coach.right) * 1.4f, -1f, 1f);
                _audio.PlayCue(AudioCue.RoadEdgeBump, 1f, pan);
            } else if (!_edgeArmed && offCenter <= EdgeRearm) {
                _edgeArmed = true;
            }
        }

        // The burning stretches the game tracks per road tile.
        private void CheckDanger() {
            bool danger = RoadMaterialUtils.InInkfireTile;
            if (!_dangerKnown) {
                _dangerKnown = true;
                _inDanger = danger;
                return;
            }
            if (danger != _inDanger) {
                _inDanger = danger;
                _audio.PlayCue(danger ? AudioCue.RoadDangerEnter : AudioCue.RoadDangerExit, 1f, 0f);
            }
        }

        // Each junction announces once, as its banners come up on approach; the route menu
        // itself opens when the coach halts there.
        private void AnnounceApproachingFork() {
            foreach (var intersection in UnityEngine.Object.FindObjectsOfType<TriggerIntersectionBhv>()) {
                var state = StateField(intersection);
                if (state != IntersectionState.ENTER && state != IntersectionState.SLOW_DOWN) {
                    continue;
                }
                if (_announcedForks.Add(intersection.GetInstanceID())) {
                    _audio.PlayCue(AudioCue.RoadFork, 1f, 0f);
                    _speak(S.RoadForkAhead, false);
                }
            }
        }

    }
}
