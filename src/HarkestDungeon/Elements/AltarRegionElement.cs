using System.Collections.Generic;
using Assets.Code.AltarOfHope;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One region marker on the Altar of Hope map ("The Working Fields", "The Living City"):
    /// named by the game's own region string, "unavailable" while the game has the marker
    /// disabled (locked regions, the intro's restriction to one region). Enter is the game's
    /// own submit, which opens the region's sub-screen.
    /// </summary>
    public sealed class AltarRegionElement : SelectableElement {
        public AltarRegionElement(AltarRegionTag region, Selectable selectable)
            : base(selectable, () => GameLoc.TryGet("altar_region_" + region.RegionKey + "_name")) {
        }

        // The game locks a region by disabling its Selectable COMPONENT (not the object and
        // not interactable), which the base reads do not see.
        public override string Value => Selectable != null && !Selectable.enabled ? S.StatusUnavailable : null;

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.enabled) {
                yield break;
            }
            foreach (var action in base.GetActions()) {
                yield return action;
            }
        }
    }
}
