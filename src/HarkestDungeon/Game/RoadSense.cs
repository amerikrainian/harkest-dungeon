using System;
using System.Collections.Generic;
using Assets.Code.Actor.Events;
using Assets.Code.Bark.Events;
using Assets.Code.Events;
using Assets.Code.Game;
using Assets.Code.Game.StageCoach;
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
    /// louder one-shot announcing its first appearance) - each re-aimed every frame, hitbox to
    /// hitbox: distance and pan run between the closest points of the object's trigger zone and
    /// the coach's own body, so a wide zone whose edge is dead ahead sounds centered and the
    /// coach's bulk counts against the gap, louder as it nears, so the whole nearby layout stays
    /// audible and steering reflects immediately. Candidate discovery (the allocating scene sweep) stays on a slow
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
        private static readonly AccessTools.FieldRef<TileNodeBhv, Collider[]> IgnoredNodeCollidersField =
            AccessTools.FieldRefAccess<TileNodeBhv, Collider[]>(TileNodeBhv.VAR_IGNORED_NODE_COLLIDERS);

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
        // Turn strength (the applied turn rate as a fraction of the coach's max) past which the
        // turning loop starts, and under which it ends - the gap keeps road-following
        // micro-corrections from chattering the loop.
        private const float TurnStart = 0.25f;
        private const float TurnStop = 0.12f;

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
        // Hitbox geometry per candidate, rebuilt on the scan clock alongside the candidate
        // arrays (live Collider references, queried per frame). Extent is how far the object's
        // collider points reach from its transform - the pre-cull radius that keeps the
        // physics closest-point queries to candidates actually near the coach.
        private readonly struct Hitbox {
            public Hitbox(Collider[] colliders, float extent) {
                Colliders = colliders;
                Extent = extent;
            }
            public readonly Collider[] Colliders;
            public readonly float Extent;
        }
        private readonly Dictionary<int, Hitbox> _hitboxes = new Dictionary<int, Hitbox>();
        private readonly List<Collider> _colliderScratch = new List<Collider>();
        private Collider[] _coachColliders = new Collider[0];
        private float _coachExtent;
        private readonly HashSet<int> _hitboxWarned = new HashSet<int>();
        private readonly Dictionary<int, IAudioLoop> _pickupLoops = new Dictionary<int, IAudioLoop>();
        private readonly Dictionary<int, IAudioLoop> _nodeLoops = new Dictionary<int, IAudioLoop>();
        private readonly HashSet<int> _staleLoops = new HashSet<int>();
        private bool _edgeArmed = true;
        private bool _inDanger;
        private bool _dangerKnown;
        private IAudioLoop _turnLoop;

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
                _hitboxes.Clear();
                _coachColliders = new Collider[0];
                _coachExtent = 0f;
                _hitboxWarned.Clear();
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
            CheckTurning(vehicle);
            CheckDanger();

            if (Time.unscaledTime < _nextScanTime) {
                return;
            }
            _nextScanTime = Time.unscaledTime + ScanInterval;
            // The allocating sweeps: new objects stream in with the road tiles, so a slow
            // refresh is enough; everything per-frame reads these arrays.
            _pickupCandidates = UnityEngine.Object.FindObjectsOfType<RoadEventBhv>();
            _nodeCandidates = UnityEngine.Object.FindObjectsOfType<TileNodeBhv>();
            RefreshHitboxes(vehicle);
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
                float limit = _pickupLoops.ContainsKey(id) ? range * 1.1f : range;
                Aim(coach, pickup.transform, _hitboxes[id], limit, out float distance, out float pan);
                if (distance > limit) {
                    continue;
                }
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
                float limit = _nodeLoops.ContainsKey(id) ? range * 1.1f : range;
                Aim(coach, node.transform, _hitboxes[id], limit, out float distance, out float pan);
                if (distance > limit) {
                    continue;
                }
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

        // The turning layer: the loop runs while the coach actually rotates - the game's own
        // turn-speed state times the speed ratio, the same product its rotation math applies
        // (and what the road-snap assist steers with, so curves the coach takes itself sound
        // too) - panned toward the turn and louder the harder it is. The settle back to
        // straight plays the end cue; a loop cut by a capture or mode change ends silently.
        private void CheckTurning(AVehicleControl vehicle) {
            float ratio = vehicle is StageCoachVehicleControlBhv coach
                ? coach.GetTurnSpeedRatio() * Mathf.Abs(vehicle.GetSpeedRatio()) : 0f;
            float strength = Mathf.Abs(ratio);
            // A positive turn ratio is a LEFT turn (ear-verified), so the pan negates it.
            float pan = Mathf.Clamp(-ratio * 1.4f, -1f, 1f);
            float volume = Mathf.Lerp(0.35f, 0.9f, strength);
            if (_turnLoop == null) {
                if (strength > TurnStart) {
                    _turnLoop = _audio.StartLoop(AudioCue.RoadTurning, volume, pan);
                }
                return;
            }
            if (strength < TurnStop) {
                _turnLoop.Stop();
                _turnLoop = null;
                _audio.PlayCue(AudioCue.RoadTurnEnd, 1f, 0f);
                return;
            }
            _turnLoop.Update(volume, pan);
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
            if (_turnLoop != null) {
                _turnLoop.Stop();
                _turnLoop = null;
            }
        }

        // Distance and pan between the closest points on the target's trigger colliders and
        // the coach's body colliders - the same geometry the game's own collection test runs
        // on - so a wide zone whose edge is dead ahead sounds centered and the coach's own
        // width counts against the gap. A cheap center-distance bound culls the far majority
        // of candidates before any physics query; a culled target answers an infinite
        // distance, which every caller's range gate drops.
        private void Aim(Transform coach, Transform target, Hitbox hitbox, float cullRange,
                         out float distance, out float pan) {
            Vector3 center = target.position;
            if (Vector3.Distance(coach.position, center) - hitbox.Extent - _coachExtent > cullRange) {
                distance = float.MaxValue;
                pan = 0f;
                return;
            }
            Vector3 targetPoint = ClosestOn(hitbox.Colliders, coach.position, center);
            Vector3 coachPoint = ClosestOn(_coachColliders, targetPoint, coach.position);
            targetPoint = ClosestOn(hitbox.Colliders, coachPoint, center);
            Vector3 delta = targetPoint - coachPoint;
            distance = delta.magnitude;
            pan = PanOf(coach, delta);
        }

        // Touching or overlapping hitboxes read as dead ahead.
        private static float PanOf(Transform coach, Vector3 delta) {
            if (delta.sqrMagnitude < 1e-4f) {
                return 0f;
            }
            Vector3 to = delta.normalized;
            float angle = Mathf.Atan2(Vector3.Dot(to, coach.right), Vector3.Dot(to, coach.forward));
            return Mathf.Clamp(Mathf.Sin(angle) * 1.4f, -1f, 1f);
        }

        // Closest point on any of the colliders to the query point; dead, disabled, and
        // inactive colliders are skipped, and a set with none usable answers the fallback
        // (the owner's transform position).
        private static Vector3 ClosestOn(Collider[] colliders, Vector3 point, Vector3 fallback) {
            float bestSqr = float.MaxValue;
            Vector3 best = fallback;
            foreach (var collider in colliders) {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) {
                    continue;
                }
                Vector3 candidate = SupportsExactClosest(collider)
                    ? collider.ClosestPoint(point) : collider.ClosestPointOnBounds(point);
                float sqr = (candidate - point).sqrMagnitude;
                if (sqr < bestSqr) {
                    bestSqr = sqr;
                    best = candidate;
                }
            }
            return best;
        }

        // Collider.ClosestPoint supports the primitive shapes and convex meshes only;
        // anything else (wheels, concave meshes on the coach) uses its AABB instead.
        private static bool SupportsExactClosest(Collider collider) {
            return collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider
                || (collider is MeshCollider mesh && mesh.convex);
        }

        // The scan-clock side of the hitbox math: the coach's body colliders and every
        // candidate's trigger colliders, rebuilt with the candidate arrays they parallel.
        private void RefreshHitboxes(AVehicleControl vehicle) {
            _coachColliders = SweepCoachColliders(vehicle);
            _coachExtent = ExtentFrom(vehicle.transform.position, _coachColliders);
            if (_coachColliders.Length == 0 && _hitboxWarned.Add(vehicle.GetInstanceID())) {
                Plugin.Log.LogWarning("RoadSense: no solid colliders under the vehicle; aiming from its transform");
            }
            _hitboxes.Clear();
            foreach (var pickup in _pickupCandidates) {
                if (pickup != null) {
                    _hitboxes[pickup.GetInstanceID()] = BuildHitbox(pickup, PickupTriggers(pickup));
                }
            }
            foreach (var node in _nodeCandidates) {
                if (node != null) {
                    _hitboxes[node.GetInstanceID()] = BuildHitbox(node, NodeTriggers(node));
                }
            }
        }

        private Hitbox BuildHitbox(Component owner, Collider[] colliders) {
            if (colliders.Length == 0 && _hitboxWarned.Add(owner.GetInstanceID())) {
                Plugin.Log.LogWarning($"RoadSense: no trigger colliders under {owner.name}; aiming at its transform");
            }
            return new Hitbox(colliders, ExtentFrom(owner.transform.position, colliders));
        }

        private static float ExtentFrom(Vector3 origin, Collider[] colliders) {
            float max = 0f;
            foreach (var collider in colliders) {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) {
                    continue;
                }
                var bounds = collider.bounds;
                float reach = (bounds.center - origin).magnitude + bounds.extents.magnitude;
                if (reach > max) {
                    max = reach;
                }
            }
            return max;
        }

        // A road event's zone is the trigger colliders under it - what its OnTriggerEnter
        // listens through.
        private Collider[] PickupTriggers(RoadEventBhv pickup) {
            _colliderScratch.Clear();
            foreach (var collider in pickup.GetComponentsInChildren<Collider>(true)) {
                if (collider.isTrigger) {
                    _colliderScratch.Add(collider);
                }
            }
            return _colliderScratch.ToArray();
        }

        // The node collision rule TileNodeBhv.Start wires: its own collider plus child
        // triggers on its layer, minus the colliders it explicitly ignores for collision
        // (oversized narration and road-sfx zones).
        private Collider[] NodeTriggers(TileNodeBhv node) {
            _colliderScratch.Clear();
            Collider[] ignored = IgnoredNodeCollidersField(node);
            foreach (var collider in node.GetComponentsInChildren<Collider>(true)) {
                if (!collider.isTrigger) {
                    continue;
                }
                if (collider.gameObject != node.gameObject
                    && (collider.gameObject.layer != node.gameObject.layer
                        || (ignored != null && Array.IndexOf(ignored, collider) >= 0))) {
                    continue;
                }
                _colliderScratch.Add(collider);
            }
            return _colliderScratch.ToArray();
        }

        // The coach's physical bulk: every solid collider under the vehicle assembly - the
        // control rig and horses, plus the wagon body wherever the game parents it. The
        // assembly's trigger colliders are listeners, not the body.
        private Collider[] SweepCoachColliders(AVehicleControl vehicle) {
            _colliderScratch.Clear();
            AddSolidColliders(vehicle.gameObject);
            GameObject coachGObj = vehicle is StageCoachVehicleControlBhv coach ? coach.GetCoachGObj() : null;
            if (coachGObj != null && !coachGObj.transform.IsChildOf(vehicle.transform)) {
                AddSolidColliders(coachGObj);
            }
            return _colliderScratch.ToArray();
        }

        private void AddSolidColliders(GameObject root) {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) {
                if (!collider.isTrigger) {
                    _colliderScratch.Add(collider);
                }
            }
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
