using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One hero tile on the inn's relationship matrix: the hero's name and class (the identity
    /// the portrait carries), then the anchor's relationship to them as the tile draws it - the
    /// band word with the affinity meter ("Neutral, 11/20"), plus the formed relationship's
    /// remaining days while the tile displays them. Enter is the game's own click: it re-anchors
    /// the matrix on this hero.
    /// </summary>
    public sealed class RelationshipMatrixTileElement : SelectableElement {
        private static readonly AccessTools.FieldRef<RelationshipMatrixActorBhv, SimpleRelationshipGraphBhv> GraphField =
            AccessTools.FieldRefAccess<RelationshipMatrixActorBhv, SimpleRelationshipGraphBhv>("m_relationshipGraph");
        private static readonly AccessTools.FieldRef<RelationshipMatrixActorBhv, DataContextBhv> TileContextField =
            AccessTools.FieldRefAccess<RelationshipMatrixActorBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<RelationshipMatrixActorBhv, GameObject> DurationObjField =
            AccessTools.FieldRefAccess<RelationshipMatrixActorBhv, GameObject>("m_relationshipDurationObj");
        private static readonly AccessTools.FieldRef<SimpleRelationshipGraphBhv, DataContextBhv> GraphContextField =
            AccessTools.FieldRefAccess<SimpleRelationshipGraphBhv, DataContextBhv>("m_dataContextBhv");

        private readonly RelationshipMatrixActorBhv _tile;

        public RelationshipMatrixTileElement(RelationshipMatrixActorBhv tile, Selectable selectable)
            : base(selectable, () => Identity(tile)) {
            _tile = tile;
        }

        private static string Identity(RelationshipMatrixActorBhv tile) {
            var actor = Actors.Get(tile.ActorGuid);
            if (actor == null) {
                return null;
            }
            return SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id));
        }

        // The band word and meter bind on the graph's context, the countdown on the tile's own;
        // the countdown widget's visibility is the game's has-a-relationship gate.
        public override string Value {
            get {
                if (_tile == null) {
                    return null;
                }
                var graph = GraphField(_tile);
                var graphContext = graph == null ? null : GraphContextField(graph);
                if (graphContext == null) {
                    return null;
                }
                string band = GameLoc.TryGet(graphContext.GetStringValue("relationship_label"));
                string number = graphContext.GetStringValue("relationship_number");
                var durationObj = DurationObjField(_tile);
                string duration = durationObj != null && durationObj.activeSelf
                    ? TileContextField(_tile).GetStringValue("relationship_duration")
                    : null;
                return SpokenLine.Join(band, number, duration);
            }
        }
    }
}
