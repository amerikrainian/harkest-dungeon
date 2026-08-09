using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Affinity;
using Assets.Code.Affinity.Events;
using Assets.Code.Events;
using Assets.Code.Game;
using Assets.Code.Inn.Events;
using Assets.Code.Library;
using Assets.Code.Quirk;
using Assets.Code.Source;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Inn transient-text lines: the pop texts and floating affinity changes the inn shows
    /// around rest items, composed at event time into a pending queue the inn screen's pump
    /// drains. Bark bubbles arrive through the bark spawner patch (<see cref="BarkEvents"/>
    /// routes inn spawns here). Refusal lines mirror PopTextManager's rest-item handler: the
    /// blocking quirk's name, the game's condition-blocked sentence, or the blocking
    /// relationship's name, led by the refused hero's name.
    /// </summary>
    public static class InnEvents {
        private static readonly List<string> _pending = new List<string>();
        private static bool _attached;

        /// <summary>Idempotent; attached at startup so lines never wait for a first inn
        /// visit.</summary>
        public static void Attach() {
            if (_attached) {
                return;
            }
            _attached = true;
            EventManager.AddListener<EventRestItemBlocked>(HandleRestItemBlocked);
            EventManager.AddListener<EventAffinityConnectionLeaningChange>(HandleAffinityChange);
        }

        /// <summary>An already-composed line for the inn pump to announce (the bark spawner
        /// patch feeds through here).</summary>
        public static void Enqueue(string line) {
            if (!string.IsNullOrWhiteSpace(line)) {
                _pending.Add(line);
            }
        }

        public static IReadOnlyList<string> Drain() {
            if (_pending.Count == 0) {
                return null;
            }
            var drained = new List<string>(_pending);
            _pending.Clear();
            return drained;
        }

        public static void Clear() => _pending.Clear();

        private static bool AtInn => GameModeMgr.CurrentMode == GameModeType.INN;

        // The refusal pop text the game floats over the hero, with the hero's name leading so
        // the spoken line says who refused.
        private static void HandleRestItemBlocked(EventRestItemBlocked evt) {
            if (!AtInn) {
                return;
            }
            var actor = Actors.Get(evt.m_ActorGuid);
            string reason = null;
            if (evt.m_SourceType == SourceType.QUIRK || evt.m_SourceType == SourceType.DISEASE
                || evt.m_SourceType == SourceType.CURSE) {
                if (SingletonMonoBehaviour<Library<string, QuirkDefinition>>.Instance
                        .TryGetLibraryElement(evt.m_sourceId, out var quirk)) {
                    reason = QuirkDescription.GetNameString(quirk, actor);
                }
            } else if (evt.m_SourceType == SourceType.INVENTORY) {
                reason = GameLoc.TryGet("inn_rest_item_condition_blocked");
            } else if (evt.m_SourceType == SourceType.RELATIONSHIP) {
                reason = AffinityRelationshipDescription.GetName(evt.m_sourceId);
            }
            if (reason == null) {
                return;
            }
            Enqueue(SpokenLine.Join(Actors.Name(actor), reason));
        }

        // The floating affinity change over the rest slots ("Dismas and Audrey, affinity +1"),
        // the same line the combat tick speaks.
        private static void HandleAffinityChange(EventAffinityConnectionLeaningChange evt) {
            if (!AtInn || evt.m_Connection == null || evt.m_LeaningChange == 0) {
                return;
            }
            var guids = evt.m_Connection.ActorGuids;
            if (guids == null || guids.Count < 2) {
                return;
            }
            string first = Actors.Name(Actors.Get(guids[0]));
            string second = Actors.Name(Actors.Get(guids[1]));
            if (first == null || second == null) {
                return;
            }
            string change = evt.m_LeaningChange > 0
                ? "+" + evt.m_LeaningChange : evt.m_LeaningChange.ToString();
            Enqueue(S.CombatAffinity(first, second, change));
        }
    }
}
