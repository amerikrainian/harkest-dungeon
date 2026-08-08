using System.Collections.Generic;
using Assets.Code.Affinity;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One hero relationship on the embark staging screen: the two heroes' names, then the
    /// relationship's own localized name once it is applied. Before that the game shows only
    /// a question mark over the pair - spoken as the authored unrevealed value, never the
    /// pending name the game is hiding. Enter drives the game's own press, which commits the
    /// pending relationship and plays the reveal; the revealed name reads from the row
    /// afterwards, and the modified-skill tooltips land in the buffer.
    /// </summary>
    public sealed class EmbarkRelationshipElement : SelectableElement {
        private static readonly AccessTools.FieldRef<EmbarkRelationshipBtnBhv, AffinityConnection> ConnectionField =
            AccessTools.FieldRefAccess<EmbarkRelationshipBtnBhv, AffinityConnection>("m_affinityConnection");

        private readonly EmbarkRelationshipBtnBhv _button;

        public EmbarkRelationshipElement(EmbarkRelationshipBtnBhv button, Selectable selectable)
            : base(selectable, () => Names(button)) {
            _button = button;
        }

        private static string Names(EmbarkRelationshipBtnBhv button) {
            var connection = ConnectionField(button);
            if (connection == null || connection.ActorGuids == null || connection.ActorGuids.Count == 0) {
                return null;
            }
            string first = Actors.Name(Actors.Get(connection.ActorGuids[0]));
            string last = Actors.Name(Actors.Get(connection.ActorGuids[connection.ActorGuids.Count - 1]));
            return SpokenLine.Join(first, last);
        }

        public override string Value {
            get {
                var connection = ConnectionField(_button);
                if (connection == null) {
                    return base.Value;
                }
                var relationship = connection.GetCurrentRelationship();
                if (relationship != null) {
                    return GameLoc.TryGet(relationship.m_Id);
                }
                if (connection.GetHasPendingRelationship()) {
                    return S.RelationshipUnrevealed;
                }
                return base.Value;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.interactable) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, _button.OnRelationshipButtonPressed);
        }
    }
}
