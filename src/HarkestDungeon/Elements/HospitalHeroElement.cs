using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// The hospital's hero pager: the treated hero's name with HP and stress from the live
    /// actor, Left/Right paging the party through the browser's own stepping (the landed
    /// hero reads in full), the status-bar tooltip as buffer lines. The treatment rows below
    /// follow the paged hero through the screen's rebuild.
    /// </summary>
    public sealed class HospitalHeroElement : UIElement {
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, PartyBrowserBhv> BrowserField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, PartyBrowserBhv>("m_partyBrowser");
        private static readonly AccessTools.FieldRef<PartyBrowserBhv, uint> ActorGuidField =
            AccessTools.FieldRefAccess<PartyBrowserBhv, uint>("m_CurrentActorGuid");

        private readonly HospitalScreenBhv _hospital;

        public HospitalHeroElement(HospitalScreenBhv hospital) {
            _hospital = hospital;
        }

        private PartyBrowserBhv Browser => _hospital == null ? null : BrowserField(_hospital);

        /// <summary>The paged hero, for the screen's rebuild signature.</summary>
        public uint ActorGuid {
            get {
                var browser = Browser;
                return browser == null ? 0 : ActorGuidField(browser);
            }
        }

        private ActorInstance Actor {
            get {
                var browser = Browser;
                return browser == null ? null : Actors.Get(ActorGuidField(browser));
            }
        }

        public override bool CanFocus => Actor != null;

        public override string Label => Actors.Name(Actor);

        public override string Role => S.RoleHero;

        public override string Value => Actors.StatusLine(Actor);

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Increase, () => Browser.Next());
            yield return new ElementAction(ActionIds.Decrease, () => Browser.Previous());
        }

        // A page reads the landed hero in full; the browser wraps, it has no ends.
        public override string GetAdjustText(string actionId, bool changed) => GetFocusText();

        protected override IEnumerable<string> GetDetailLines() {
            var browser = Browser;
            if (browser != null) {
                foreach (var line in TooltipReader.Lines(browser.gameObject)) {
                    yield return line;
                }
            }
        }
    }
}
