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
    /// disabled (locked regions, the intro's restriction to one region). A locked region's
    /// buffer carries the sub-screen's own unlock requirement, the text the sighted lock
    /// tooltip shows. Enter is the game's own submit, which opens the region's sub-screen.
    /// </summary>
    public sealed class AltarRegionElement : SelectableElement {
        private readonly AltarRegionTag _region;

        public AltarRegionElement(AltarRegionTag region, Selectable selectable)
            : base(selectable, () => GameLoc.TryGet("altar_region_" + region.RegionKey + "_name")) {
            _region = region;
        }

        // The game locks a region by disabling its Selectable COMPONENT (not the object and
        // not interactable), which the base reads do not see.
        public override string Status => Selectable != null && !Selectable.enabled ? S.StatusUnavailable : base.Status;

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.enabled) {
                yield break;
            }
            foreach (var action in base.GetActions()) {
                yield return action;
            }
        }

        protected override IEnumerable<string> GetDetailLines() {
            foreach (var line in base.GetDetailLines()) {
                yield return line;
            }
            if (Selectable != null && !Selectable.enabled && _region.SubScreenPrefab != null) {
                var sub = _region.SubScreenPrefab
                    .GetComponent<Assets.Code.UI.Widgets.SubScreenElementBhv>();
                string lockedKey = sub == null ? null : sub.GetLockedString();
                string reason = string.IsNullOrEmpty(lockedKey) ? null : GameLoc.TryGet(lockedKey);
                if (!string.IsNullOrEmpty(reason)) {
                    yield return reason;
                }
            }
        }
    }
}
