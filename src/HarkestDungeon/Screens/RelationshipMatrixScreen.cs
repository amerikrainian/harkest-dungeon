using Assets.Code.UI;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn's Relationships matrix (a <c>SubScreenRelationshipMatrixBhv</c> stack entry,
    /// Kingdoms), named by the inn header's station title. The anchor hero reads first (name
    /// and class - the panel the portrait grid pivots around), then one tile per other roster
    /// hero with the anchor's relationship to them. Enter re-anchors the matrix on the focused
    /// hero; the rebuild lands back on the anchor readout, which names the new anchor.
    /// </summary>
    public sealed class RelationshipMatrixScreen : GameScreen {
        private SubScreenRelationshipMatrixBhv _panel;
        private Container _root;
        private int _builtSignature;

        public override string Name => InnStations.Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<SubScreenRelationshipMatrixBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (SubScreenRelationshipMatrixBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (panel.GoBack()) {
                    panel.CloseSubscreen();
                }
            });
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (SubScreenRelationshipMatrixBhv)target;
            if (Signature(panel) != _builtSignature) {
                _root.Clear();
                Populate(panel);
            }
            return false;
        }

        private void Populate(SubScreenRelationshipMatrixBhv panel) {
            _root.Add(new ReadoutElement(() => {
                var actor = Actors.Get(panel.ActiveActorGuid);
                if (actor == null) {
                    return null;
                }
                return SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id));
            }));
            foreach (var tile in panel.GetComponentsInChildren<RelationshipMatrixActorBhv>(includeInactive: false)) {
                // The anchor's own cell is the grid's "you are here" mark; the readout above
                // already names it.
                if (tile.ActorGuid == panel.ActiveActorGuid) {
                    continue;
                }
                var selectable = tile.GetComponent<Selectable>();
                if (selectable != null) {
                    _root.Add(new RelationshipMatrixTileElement(tile, selectable));
                }
            }
            _builtSignature = Signature(panel);
        }

        // The tile pool hands the same instances back on every re-anchor, so the signature keys
        // on the anchor and each tile's hero, not instance ids alone.
        private static int Signature(SubScreenRelationshipMatrixBhv panel) {
            int signature = 17 * 31 + (int)panel.ActiveActorGuid;
            foreach (var tile in panel.GetComponentsInChildren<RelationshipMatrixActorBhv>(includeInactive: false)) {
                signature = signature * 31 + tile.GetInstanceID();
                signature = signature * 31 + (int)tile.ActorGuid;
            }
            return signature;
        }
    }
}
