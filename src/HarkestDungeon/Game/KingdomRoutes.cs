using System.Collections.Generic;
using Assets.Code.Inn;
using Assets.Code.Kingdom;
using Assets.Code.Utils;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// The stagecoach's travel graph, walked by the game's own hop rules. A day's move is a
    /// straight two-cell hop to an inn, camp, or (conditions met) the boss cell across an
    /// active region - the game's AreCoordinatesValidForStagecoachTravel - or an underground
    /// hop from an inn with the network to any other visited network inn - its
    /// AreCoordinatesValidForStagecoachFastTravel; either books one travel, and the day
    /// counter moves once per travel. Distances count from the booked destination while a
    /// trip is scheduled, as the game's own days-away helper does, on the map as it stands
    /// today: a siege, a reveal, or an unlock still to come is not foreseen, the same limit a
    /// sighted hop count has.
    /// </summary>
    public static class KingdomRoutes {
        private static readonly Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        };

        /// <summary>Days of travel from the coach to the cell: 0 at the origin, null when no
        /// hop sequence reaches it. A region is the day of the cheapest hop that crosses
        /// it.</summary>
        public static int? DaysTo(Vector2Int target) {
            var mgr = Manager();
            var distances = Distances(mgr);
            if (distances.TryGetValue(target, out int days)) {
                return days;
            }
            var cell = mgr.KingdomMap.TryGetCell(target);
            if (!(cell is KingdomMapCellBiome) || !cell.IsActive) {
                return null;
            }
            int? best = null;
            foreach (var direction in Directions) {
                if (distances.TryGetValue(target - direction, out int from)
                    && IsRoadHop(mgr, target - direction, target + direction)
                    && (best == null || from + 1 < best.Value)) {
                    best = from + 1;
                }
            }
            return best;
        }

        /// <summary>Compares the one-hop set from the coach's current cell with the game's
        /// own validity answers over every cell. A mismatch means the game's travel rule
        /// moved under the graph, and every days-away read is then suspect - logged loudly
        /// rather than spoken wrong.</summary>
        public static void VerifyAgainstGame() {
            var mgr = Manager();
            var ours = new HashSet<Vector2Int>(Hops(mgr, mgr.CurrentStageCoachCoordinates));
            var mismatches = new List<string>();
            foreach (var cell in mgr.KingdomMap.GetCells(c => c != null && c.CellType != null)) {
                bool game = mgr.AreCoordinatesValidForStagecoachTravel(cell.Coordinates)
                    || mgr.AreCoordinatesValidForStagecoachFastTravel(cell.Coordinates);
                if (game != ours.Contains(cell.Coordinates)) {
                    mismatches.Add(cell.Coordinates + (game ? " game-only" : " graph-only"));
                }
            }
            if (mismatches.Count > 0) {
                Plugin.Log.LogWarning("kingdom routes: the hop rule disagrees with the game at "
                    + string.Join(", ", mismatches.ToArray()) + "; days-away reads are suspect");
            }
        }

        private static KingdomMapManager Manager() => SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomMapManager;

        private static Dictionary<Vector2Int, int> Distances(KingdomMapManager mgr) {
            var origin = mgr.HasNextStageCoachCoordinates ? mgr.NextStageCoachCoordinates : mgr.CurrentStageCoachCoordinates;
            var distances = new Dictionary<Vector2Int, int> { [origin] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(origin);
            while (queue.Count > 0) {
                var at = queue.Dequeue();
                foreach (var next in Hops(mgr, at)) {
                    if (!distances.ContainsKey(next)) {
                        distances[next] = distances[at] + 1;
                        queue.Enqueue(next);
                    }
                }
            }
            return distances;
        }

        // Every stop one day from a cell: the four road hops, then the underground network
        // when the cell is an inn that has it.
        private static IEnumerable<Vector2Int> Hops(KingdomMapManager mgr, Vector2Int from) {
            foreach (var direction in Directions) {
                var to = from + direction * 2;
                if (IsRoadHop(mgr, from, to)) {
                    yield return to;
                }
            }
            if (!(mgr.KingdomMap.TryGetCell(from) is KingdomMapCellInnContainer origin) || origin.InnInstance == null
                || !origin.InnInstance.GetIsInnFeatureEnabled(InnFeatureType.FAST_TRAVEL)) {
                yield break;
            }
            foreach (var cell in mgr.KingdomMap.GetCells(c => c is KingdomMapCellInnContainer)) {
                var inn = (KingdomMapCellInnContainer)cell;
                if (inn.Coordinates != from && inn.IsActive && inn.InnInstance != null
                    && inn.InnInstance.GetIsValidFastTravelDestination()) {
                    yield return inn.Coordinates;
                }
            }
        }

        // The game's road rule from an arbitrary origin: straight, two cells, the region
        // between active, the far cell a stop.
        private static bool IsRoadHop(KingdomMapManager mgr, Vector2Int from, Vector2Int to) {
            var map = mgr.KingdomMap;
            var delta = to - from;
            if ((delta.x != 0 && delta.y != 0) || Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 2
                || !map.IsValidCoordinate(to)) {
                return false;
            }
            var between = map.TryGetCell(from + new Vector2Int(delta.x / 2, delta.y / 2)) as KingdomMapCellBiome;
            if (between == null || between.CellType == null || !between.IsActive) {
                return false;
            }
            return IsStop(mgr, map.TryGetCell(to));
        }

        private static bool IsStop(KingdomMapManager mgr, KingdomMapCellBase cell) {
            if (cell == null || cell.CellType == null || !cell.IsActive) {
                return false;
            }
            if (cell is KingdomMapCellBoss) {
                return mgr.AreBossCellConditionsMet();
            }
            return cell is KingdomMapCellInnContainer;
        }
    }
}
