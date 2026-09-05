using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Code.Actor.Events;
using Assets.Code.Events;
using Assets.Code.Game;
using Assets.Code.Game.StageCoach;
using Assets.Code.Item;
using Assets.Code.Item.Events;
using Assets.Code.Map;
using Assets.Code.Map.Events;
using Assets.Code.Map.RoadEvents;
using Assets.Code.Map.Triggers;
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
    /// miss). EVERY uncollected loot pickup in hearing range sounds as its own continuous
    /// loop - the one thing on the road worth steering at; the loot-less destructible debris
    /// sharing its event category stays silent - re-aimed every frame, hitbox to
    /// hitbox: distance and pan run between the closest points of the pickup's trigger zone and
    /// the coach's own body, so a wide zone whose edge is dead ahead sounds centered and the
    /// coach's bulk counts against the gap, louder as it nears, so the whole nearby layout stays
    /// audible and steering reflects immediately. Candidate discovery (the allocating scene sweep) stays on a slow
    /// clock; the per-frame work runs over the cached candidates allocation-free, and a loop
    /// cuts the frame its pickup is collected or out of range. With the auto-collect setting
    /// on, the pings fall silent and a loot pickup collects itself as the coach passes abeam,
    /// through the pickup's own physics entry point, so the game's whole drive-over sequence
    /// (gates, vfx, loot toast) runs unchanged. The coach's own motion keeps two
    /// cues: the turning loop and the road-edge bump. Everything else on the road is speech -
    /// collection, damage, barks (via the bark-spawner patches, BarkEvents). Event listeners
    /// and patches only compose pending lines; every sound and word
    /// goes out on the pump path.
    /// </summary>
    public sealed class RoadSense {
        private static readonly AccessTools.FieldRef<RoadEventBhv, RoadEventInteractionType> InteractionTypeField =
            AccessTools.FieldRefAccess<RoadEventBhv, RoadEventInteractionType>("m_interactionType");
        private static readonly AccessTools.FieldRef<RoadEventBhv, int> CollisionCountField =
            AccessTools.FieldRefAccess<RoadEventBhv, int>("m_collisionCount");
        // The pickup's own physics entry and exit points - the exact calls a real drive-over
        // makes, so every game-side gate and listener behaves identically.
        private static readonly MethodInfo TriggerEnterMethod =
            AccessTools.Method(typeof(RoadEventBhv), "OnTriggerEnter");
        private static readonly MethodInfo TriggerExitMethod =
            AccessTools.Method(typeof(RoadEventBhv), "OnTriggerExit");

        // The sensing radius is the player's setting, read live per scan.
        private readonly Core.Settings.IntSetting _sensingRange;
        private const float ScanInterval = 0.7f;
        // A pickup only counts as passed after having been clearly ahead, so enabling the
        // setting mid-drive never collects what already lies abeam or behind.
        private const float PassAheadMargin = 1f;
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
        private float _nextScanTime;
        private RoadEventBhv[] _pickupCandidates = new RoadEventBhv[0];
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
        private readonly HashSet<int> _staleLoops = new HashSet<int>();
        private bool _edgeArmed = true;
        private IAudioLoop _turnLoop;

        // Auto collect: the setting, the real pickups (loot-granting drive-through events,
        // refreshed with the candidates - what both the ping and the collector treat as a
        // pickup; the road also spawns loot-less destructible debris in the same event
        // category, which neither voices), the ones seen clearly ahead, and the synthetic
        // enters awaiting their balancing exit.
        private readonly Core.Settings.BoolSetting _autoCollect;
        private readonly bool _canCollect;
        private readonly HashSet<int> _collectible = new HashSet<int>();
        private readonly HashSet<int> _aheadPickups = new HashSet<int>();
        private readonly struct PendingExit {
            public PendingExit(RoadEventBhv roadEvent, Collider collider, int enterFrame) {
                Event = roadEvent;
                Collider = collider;
                EnterFrame = enterFrame;
            }
            public readonly RoadEventBhv Event;
            public readonly Collider Collider;
            public readonly int EnterFrame;
        }
        private readonly List<PendingExit> _pendingExits = new List<PendingExit>();

        public RoadSense(IAudioEngine audio, Action<string, bool> speak, InputGate gate,
                         Core.Settings.IntSetting sensingRange, Core.Settings.BoolSetting autoCollect) {
            _audio = audio;
            _speak = speak;
            _gate = gate;
            _sensingRange = sensingRange;
            _autoCollect = autoCollect;
            _canCollect = TriggerEnterMethod != null && TriggerExitMethod != null;
            if (!_canCollect) {
                Plugin.Log.LogError("RoadSense: RoadEventBhv trigger methods not found; auto collect disabled");
            }
            EventManager.AddListener<EventLootToastPresented>(HandleLootToast);
            EventManager.AddListener<EventActorHealthDamage>(HandleDamage);
            EventManager.AddListener<EventRunResist>(HandleRunResist);
            EventManager.AddListener<EventBiomeTitleStateChanged>(HandleBiomeTitle);
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
                null, damage == 1 ? S.CombatTookDamageOne(name) : S.CombatTookDamage(name, damage)));
        }

        // The region title card that plays on crossing a gate hides the HUD but keeps the
        // driving mode, so the driving screen stays attached and nothing re-announces: the
        // card's own biome name (the string the gate stamps on its label) speaks the crossing.
        private void HandleBiomeTitle(EventBiomeTitleStateChanged evt) {
            if (!OnRoad || !evt.m_isDisplaying) {
                return;
            }
            string name = GameLoc.TryGet("biome_name_" + GameTypeMgr.ActiveBiome);
            if (name != null) {
                _pending.Add(new KeyValuePair<AudioCue?, string>(null, name));
            }
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

        public void Tick() {
            if (!OnRoad) {
                _pending.Clear();
                _pickupCandidates = new RoadEventBhv[0];
                _hitboxes.Clear();
                _coachColliders = new Collider[0];
                _coachExtent = 0f;
                _hitboxWarned.Clear();
                _collectible.Clear();
                _aheadPickups.Clear();
                _pendingExits.Clear();
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

            // Balancing exits resolve even while a screen holds the keyboard - a collection
            // in flight finishes regardless of what opens over it.
            TickPendingExits();

            var vehicle = SingletonMonoBehaviour<MapMgrBhv>.HasInstance()
                ? SingletonMonoBehaviour<MapMgrBhv>.Instance.GetVehicleControl() : null;
            if (vehicle == null) {
                StopAllLoops();
                return;
            }
            // The pass detector also runs while a screen holds the keyboard - it makes no
            // sound, and the coach can still be rolling under a just-opened panel.
            bool collecting = _canCollect && _autoCollect.Value;
            if (collecting) {
                AutoCollectPassed(vehicle.transform);
            }

            // A captured screen owns the moment; the ambient layer stays quiet.
            if (_gate.Captured) {
                StopAllLoops();
                return;
            }
            // The continuous layers re-aim every frame over the cached candidates, so steering
            // lands in the ears the same frame it lands on the road.
            if (collecting) {
                StopPickupLoops();
            } else {
                UpdatePickupLoops(vehicle.transform);
            }
            CheckRoadEdge(vehicle.transform);
            CheckTurning(vehicle);

            if (Time.unscaledTime < _nextScanTime) {
                return;
            }
            _nextScanTime = Time.unscaledTime + ScanInterval;
            // The allocating sweep: new objects stream in with the road tiles, so a slow
            // refresh is enough; everything per-frame reads this array.
            _pickupCandidates = UnityEngine.Object.FindObjectsOfType<RoadEventBhv>();
            RefreshHitboxes(vehicle);
        }

        // One loop per uncollected loot pickup in range, each re-aimed every frame; the volume
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
                    || pickup.GetTriggerState() != TriggerState.None
                    || !_collectible.Contains(pickup.GetInstanceID())) {
                    continue;
                }
                int id = pickup.GetInstanceID();
                float range = _sensingRange.Value;
                float limit = _pickupLoops.ContainsKey(id) ? range * 1.1f : range;
                Aim(coach, pickup.transform, _hitboxes[id], limit, out float distance, out float pan, out _);
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

        private void StopPickupLoops() {
            foreach (var pair in _pickupLoops) {
                pair.Value.Stop();
            }
            _pickupLoops.Clear();
        }

        private void StopAllLoops() {
            StopPickupLoops();
            if (_turnLoop != null) {
                _turnLoop.Stop();
                _turnLoop = null;
            }
        }

        // A collectible pickup first seen clearly ahead is taken the moment it passes abeam
        // within the road's width - the reach a sighted driver covers by steering, and far
        // under the gap to a parallel branch (whose events the game deactivates on route
        // selection anyway). The take is the pickup's own physics entry point with a coach
        // collider, so the game's whole drive-over sequence runs unchanged: its own gates,
        // vfx and sfx, the loot grant, and the corner toast that already drives the spoken
        // item title.
        private void AutoCollectPassed(Transform coach) {
            if (_coachColliders.Length == 0) {
                return;
            }
            foreach (var pickup in _pickupCandidates) {
                if (pickup == null || !pickup.gameObject.activeInHierarchy
                    || pickup.RoadEventCategory != RoadEventCategory.OBJECTS
                    || pickup.GetTriggerState() != TriggerState.None) {
                    continue;
                }
                int id = pickup.GetInstanceID();
                if (!_collectible.Contains(id)) {
                    continue;
                }
                float reach = GameConstants.ROAD_SIZE;
                Aim(coach, pickup.transform, _hitboxes[id], reach, out float distance, out _, out float forward);
                if (distance > reach) {
                    continue;
                }
                if (forward > PassAheadMargin) {
                    _aheadPickups.Add(id);
                } else if (forward <= 0f && _aheadPickups.Remove(id)) {
                    Collect(pickup);
                }
            }
        }

        private void Collect(RoadEventBhv pickup) {
            // A collider already inside the zone means the coach is physically on the pickup;
            // the game's own collection is taking it.
            if (CollisionCountField(pickup) > 0) {
                return;
            }
            TriggerEnterMethod.Invoke(pickup, new object[] { _coachColliders[0] });
            _pendingExits.Add(new PendingExit(pickup, _coachColliders[0], Time.frameCount));
        }

        // The balancing exit for each synthetic enter, once the event resolves - the same
        // enter/exit lifecycle a physical drive-over produces, so the zone's collision count
        // never wedges open. Completed is the normal end; an event still untriggered a frame
        // after the enter was declined by the game's own interaction gate, which a
        // loot-granting drive-through pickup is never expected to do.
        private void TickPendingExits() {
            for (int i = _pendingExits.Count - 1; i >= 0; i--) {
                var pending = _pendingExits[i];
                if (pending.Event == null || !pending.Event.gameObject.activeInHierarchy) {
                    _pendingExits.RemoveAt(i);
                    continue;
                }
                var state = pending.Event.GetTriggerState();
                bool declined = state == TriggerState.None && Time.frameCount > pending.EnterFrame + 1;
                if (state != TriggerState.Completed && !declined) {
                    continue;
                }
                TriggerExitMethod.Invoke(pending.Event, new object[] { pending.Collider });
                _pendingExits.RemoveAt(i);
                if (declined) {
                    Plugin.Log.LogWarning($"RoadSense: auto collect declined by {pending.Event.name}");
                }
            }
        }

        // Distance, pan, and forward reach between the closest points on the target's trigger
        // colliders and the coach's body colliders - the same geometry the game's own
        // collection test runs on - so a wide zone whose edge is dead ahead sounds centered
        // and the coach's own width counts against the gap. Forward is the gap along the
        // coach's heading: positive ahead, zero or negative abeam and behind. A cheap
        // center-distance bound culls the far majority of candidates before any physics
        // query; a culled target answers an infinite distance, which every caller's range
        // gate drops.
        private void Aim(Transform coach, Transform target, Hitbox hitbox, float cullRange,
                         out float distance, out float pan, out float forward) {
            Vector3 center = target.position;
            if (Vector3.Distance(coach.position, center) - hitbox.Extent - _coachExtent > cullRange) {
                distance = float.MaxValue;
                pan = 0f;
                forward = 0f;
                return;
            }
            Vector3 targetPoint = ClosestOn(hitbox.Colliders, coach.position, center);
            Vector3 coachPoint = ClosestOn(_coachColliders, targetPoint, coach.position);
            targetPoint = ClosestOn(hitbox.Colliders, coachPoint, center);
            Vector3 delta = targetPoint - coachPoint;
            distance = delta.magnitude;
            pan = PanOf(coach, delta);
            forward = Vector3.Dot(delta, coach.forward);
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
        // candidate's trigger colliders, rebuilt with the candidate array they parallel.
        private void RefreshHitboxes(AVehicleControl vehicle) {
            _coachColliders = SweepCoachColliders(vehicle);
            _coachExtent = ExtentFrom(vehicle.transform.position, _coachColliders);
            if (_coachColliders.Length == 0 && _hitboxWarned.Add(vehicle.GetInstanceID())) {
                Plugin.Log.LogWarning("RoadSense: no solid colliders under the vehicle; aiming from its transform");
            }
            _hitboxes.Clear();
            _collectible.Clear();
            foreach (var pickup in _pickupCandidates) {
                if (pickup == null) {
                    continue;
                }
                _hitboxes[pickup.GetInstanceID()] = BuildHitbox(pickup, PickupTriggers(pickup));
                // A real pickup is a loot-granting drive-through event; the ping and auto
                // collect both key on this. An OBSTACLE event force-stops the coach on
                // interact, and an event without a loot trigger grants nothing - the final
                // climb's destructible debris shares the OBJECTS category but only crumbles.
                if (InteractionTypeField(pickup) == RoadEventInteractionType.DRIVE_THROUGH
                    && pickup.GetComponent<TriggerItemBhv>() != null) {
                    _collectible.Add(pickup.GetInstanceID());
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

    }
}
