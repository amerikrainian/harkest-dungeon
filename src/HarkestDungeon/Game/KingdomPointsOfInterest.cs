using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Kingdom;
using Assets.Code.Kingdom.Presentation;
using Assets.Code.Utils;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// The kingdom map's points of interest, say-the-spire2's map reviewer carried over: the
    /// cells of one category in reading order (row by row, left to right), optionally only the
    /// reachable ones - from the stagecoach, or from the hero the game is waiting on a
    /// destination for, whose range the game itself limits selection to. Categories cycle past
    /// empty ones. Everything is read live from the map on each step, so a day turn, a siege,
    /// or a scheduled trip never leaves a stale list; a cell whose view is hidden (the
    /// unrevealed boss ring) is never listed, as it reads as empty ground.
    /// </summary>
    public sealed class KingdomPointsOfInterest {
        private sealed class Category {
            public readonly Func<string> Label;
            public readonly Func<KingdomMapCellBase, bool> Matches;

            public Category(Func<string> label, Func<KingdomMapCellBase, bool> matches) {
                Label = label;
                Matches = matches;
            }
        }

        // Labels come from the game's own map legend where it has one (the console layout's
        // selection captions and the siege label), else the mod's cell words.
        private static readonly Category[] Categories = {
            new Category(() => GameLoc.TryGet("kingdom_map_selection_inn_label") ?? S.ScreenInn,
                cell => cell is KingdomMapCellInn),
            new Category(() => GameLoc.TryGet("kingdom_map_selection_camps_label") ?? S.ScreenInn,
                cell => cell is KingdomMapCellCamp),
            new Category(() => GameLoc.TryGet("kingdom_siege_label") ?? S.KingdomSiege,
                cell => SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomSiegeManager
                    .GetIsSiegeAtCoordinates(cell.Coordinates)),
            new Category(() => S.InnStationedHeroes, cell => cell.GetNumberOfActors() > 0),
            new Category(() => S.KingdomTreasure, cell => {
                var context = CellContext(cell.Coordinates);
                return context != null && context.GetBoolValue("treasure_active");
            }),
            new Category(() => GameLoc.TryGet("kingdom_map_selection_regions_label") ?? S.KingdomQuest,
                cell => cell is KingdomMapCellBiome),
            new Category(() => S.KingdomCursed, cell => IsCursed(cell)),
            new Category(() => S.KingdomQuest, cell => cell is KingdomMapCellBiome biome
                && ((biome.BiomeModifier != null && !IsCursed(biome)) || biome.GetHasKillContract())),
            new Category(() => S.KingdomBoss, cell => cell is KingdomMapCellBoss),
            new Category(() => GameLoc.TryGet("kingdom_map_selection_underground_travel_label") ?? S.KingdomReachable,
                cell => cell is KingdomMapCellInnContainer inn && inn.InnInstance != null
                    && inn.InnInstance.GetIsValidFastTravelDestination()),
        };

        private int _category;

        /// <summary>Whether the list holds only the cells reachable right now.</summary>
        public bool ReachableOnly { get; private set; }

        public string CategoryLabel => Categories[_category].Label();

        public string ModeLabel => ReachableOnly ? S.KingdomReachable : S.KingdomPoiAll;

        /// <summary>The current category's cells under the current filter, in reading order.</summary>
        public List<Vector2Int> Cells() => CellsOf(Categories[_category]);

        /// <summary>The cell after (direction +1) or before (-1) the anchor in reading order,
        /// null at the list's end - the anchor itself never counts.</summary>
        public Vector2Int? Step(Vector2Int anchor, int direction) {
            var cells = Cells();
            if (direction > 0) {
                foreach (var cell in cells) {
                    if (Compare(cell, anchor) > 0) {
                        return cell;
                    }
                }
            } else {
                for (int i = cells.Count - 1; i >= 0; i--) {
                    if (Compare(cells[i], anchor) < 0) {
                        return cells[i];
                    }
                }
            }
            return null;
        }

        /// <summary>Move to the next (or previous) category with any cell under the current
        /// filter, wrapping; stays put when no category has one.</summary>
        public void StepCategory(int direction) {
            for (int i = 1; i <= Categories.Length; i++) {
                int candidate = ((_category + i * direction) % Categories.Length + Categories.Length) % Categories.Length;
                if (CellsOf(Categories[candidate]).Count > 0) {
                    _category = candidate;
                    return;
                }
            }
        }

        /// <summary>Flip the reachable filter; a category the filter empties hands over to the
        /// next one that still has cells.</summary>
        public void ToggleReachable() {
            ReachableOnly = !ReachableOnly;
            if (Cells().Count == 0) {
                StepCategory(+1);
            }
        }

        private List<Vector2Int> CellsOf(Category category) {
            var mgr = SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomMapManager;
            var result = new List<Vector2Int>();
            if (mgr == null || !mgr.IsMapLoaded) {
                return result;
            }
            foreach (var cell in mgr.KingdomMap.GetCells(c => c != null && c.CellType != null && c.IsActive)) {
                if (!IsVisible(cell.Coordinates) || !category.Matches(cell)) {
                    continue;
                }
                if (ReachableOnly && !IsReachable(mgr, cell.Coordinates)) {
                    continue;
                }
                result.Add(cell.Coordinates);
            }
            result.Sort(Compare);
            return result;
        }

        // While the game waits on a destination for a hero, reachable means that hero's own
        // range; otherwise the stagecoach's next move (a neighbouring region or an unlocked
        // underground inn), which the game only offers during the player's turn.
        private static bool IsReachable(KingdomMapManager mgr, Vector2Int coords) {
            uint moving = mgr.GetSelectedTransferActorGuid();
            if (moving != 0) {
                return mgr.AreCoordinatesValidForActorTransfer(moving, coords);
            }
            if (!SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomDaySystem.IsInUserInputState()) {
                return false;
            }
            return mgr.AreCoordinatesValidForStagecoachTravel(coords)
                || mgr.AreCoordinatesValidForStagecoachFastTravel(coords);
        }

        private static bool IsCursed(KingdomMapCellBase cell)
            => cell is KingdomMapCellBiome biome && biome.BiomeModifier != null
                && biome.BiomeModifier.Tags.ContainsAll(new[] { "infection" });

        private static KingdomMapCellBhv View(Vector2Int coords) {
            var root = SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomMapManager.KingdomMapRoot;
            return root == null ? null : root[coords] as KingdomMapCellBhv;
        }

        private static bool IsVisible(Vector2Int coords) {
            var view = View(coords);
            return view != null && view.gameObject.activeInHierarchy;
        }

        private static DataContextBhv CellContext(Vector2Int coords) {
            var view = View(coords);
            return view == null ? null : view.GetComponentInChildren<DataContextBhv>(includeInactive: false);
        }

        private static int Compare(Vector2Int a, Vector2Int b) {
            int row = a.y.CompareTo(b.y);
            return row != 0 ? row : a.x.CompareTo(b.x);
        }
    }
}
