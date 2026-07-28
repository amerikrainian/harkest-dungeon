using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A hero row on the inn's Select Replacement Hero screen: the hero's name then class from
    /// the row's own bindings (the random row carries only its class label), the game's
    /// at-this-inn marker spoken as the value, and the row's add/station tooltip as buffer
    /// lines. Enter is the row's own submit, which adds or stations through the game's model.
    /// </summary>
    public sealed class InnReplacementRowElement : SelectableElement {
        private static readonly AccessTools.FieldRef<InnReplacementActorBhv, GameObject> InnMarkerField =
            AccessTools.FieldRefAccess<InnReplacementActorBhv, GameObject>("m_innActorObj");

        private readonly InnReplacementActorBhv _row;
        private readonly DataContextBhv _context;

        public InnReplacementRowElement(InnReplacementActorBhv row, Selectable selectable)
            : base(selectable) {
            _row = row;
            _context = row.GetComponent<DataContextBhv>();
        }

        public override string Label => SpokenLine.Join(
            _context.GetStringValue("actor_name"),
            _context.GetStringValue("actor_class_name"));

        public override string Value {
            get {
                var marker = InnMarkerField(_row);
                return marker != null && marker.activeSelf ? S.InnAtThisInn : null;
            }
        }
    }
}
