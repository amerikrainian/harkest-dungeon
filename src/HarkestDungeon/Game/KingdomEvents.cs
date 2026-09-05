using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Events;
using Assets.Code.Kingdom;
using Assets.Code.Kingdom.Events;
using Assets.Code.Kingdom.Presentation;
using Assets.Code.Utils;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// Kingdom map transient lines: a stationed hero's arrival at the inn they were sent to,
    /// which the game applies silently at the start of the next day (the hero's portrait
    /// simply moves cells), and the day's curse activity, which the game presents as map
    /// banners ("Contagion Released", "Contagion Spreads", "Contagion Cleansed") with the
    /// camera panning to each region concerned. Composed at event time into a pending queue
    /// the map screen's pump drains once its entry announcement is out.
    /// </summary>
    public static class KingdomEvents {
        private static readonly List<string> _pending = new List<string>();
        private static bool _attached;

        /// <summary>Idempotent; attached at startup so an arrival never waits for a first map
        /// visit.</summary>
        public static void Attach() {
            if (_attached) {
                return;
            }
            _attached = true;
            EventManager.AddListener<EventKingdomActorTransferApplied>(HandleTransferApplied);
            EventManager.AddListener<EventKingdomBiomeModifiersUpdated>(HandleBiomeModifiersUpdated);
        }

        // Each banner the game plays leads its line, the regions it concerns after it: the
        // newly cursed regions, each spread as "from X to Y", the regions cleansed. A region
        // whose view carries no name yet (undiscovered) reads by its grid position, the
        // map cursor's own words for a cell.
        internal static void HandleBiomeModifiersUpdated(EventKingdomBiomeModifiersUpdated evt) {
            if (evt.m_newSpawns != null && evt.m_newSpawns.Count > 0) {
                var names = new List<string>();
                foreach (var cell in evt.m_newSpawns) {
                    names.Add(NameOrPosition(cell.Coordinates));
                }
                AddBanner("contagion_released", names);
            }
            if (evt.m_spreadResults != null && evt.m_spreadResults.Count > 0) {
                var spreads = new List<string>();
                foreach (var spread in evt.m_spreadResults) {
                    spreads.Add(S.KingdomSpreadFromTo(NameOrPosition(spread.Origin.Coordinates),
                        NameOrPosition(spread.Destination.Coordinates)));
                }
                AddBanner("contagion_spreads", spreads);
            }
            if (evt.m_cleanseResults != null && evt.m_cleanseResults.Count > 0) {
                var names = new List<string>();
                foreach (var cleanse in evt.m_cleanseResults) {
                    names.Add(NameOrPosition(cleanse.Destination.Coordinates));
                }
                AddBanner("contagion_cleansed", names);
            }
        }

        private static string NameOrPosition(Vector2Int coords)
            => CellName(coords) ?? S.KingdomCell(coords.y + 1, coords.x + 1);

        private static void AddBanner(string bannerKey, List<string> parts) {
            string banner = GameLoc.TryGet(bannerKey);
            if (banner == null) {
                Plugin.Log.LogWarning("kingdom curse banner " + bannerKey + " has no text; the day's curse activity not spoken");
                return;
            }
            parts.Insert(0, banner);
            _pending.Add(Core.Text.SpokenLine.Join(parts.ToArray()));
        }

        public static IReadOnlyList<string> Drain() {
            if (_pending.Count == 0) {
                return null;
            }
            var drained = new List<string>(_pending);
            _pending.Clear();
            return drained;
        }

        /// <summary>The name a map cell displays (its view's bound cell_name), null for a cell
        /// without a live view.</summary>
        public static string CellName(Vector2Int coords) {
            var root = SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomMapManager.KingdomMapRoot;
            var view = root == null ? null : root[coords] as KingdomMapCellBhv;
            var context = view == null ? null : view.GetComponentInChildren<DataContextBhv>(includeInactive: false);
            return context == null ? null : context.GetStringValue("cell_name");
        }

        internal static void HandleTransferApplied(EventKingdomActorTransferApplied evt) {
            var actor = Actors.Get(evt.m_ActorGuid);
            string inn = CellName(evt.m_DestinationCoordinates);
            if (actor == null || inn == null) {
                Plugin.Log.LogWarning("kingdom arrival: hero " + evt.m_ActorGuid + " or cell "
                    + evt.m_DestinationCoordinates + " unreadable; arrival not spoken");
                return;
            }
            _pending.Add(S.KingdomHeroArrived(Actors.Name(actor), inn));
        }
    }
}
