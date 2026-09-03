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
    /// simply moves cells). Composed at event time into a pending queue the map screen's pump
    /// drains once its entry announcement is out.
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
