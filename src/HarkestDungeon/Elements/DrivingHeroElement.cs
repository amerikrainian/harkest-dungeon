using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Events;
using Assets.Code.UI.Banter;
using DD2A11y.Core.Nav;
using DD2A11y.Game;

namespace DD2A11y.Elements {
    /// <summary>
    /// One hero on the road's ribbon strip: name with HP and stress from the live actor, every
    /// ribbon tooltip (the status bar's effects, diseases) as buffer lines. Enter is the
    /// ribbon's own right-click inspect - the hero's character sheet; Space grabs the hero for
    /// a marching-order move, which the driving screen routes.
    /// </summary>
    public sealed class DrivingHeroElement : UIElement {
        private readonly HeroRibbonBhv _ribbon;

        public DrivingHeroElement(HeroRibbonBhv ribbon) {
            _ribbon = ribbon;
        }

        public HeroRibbonBhv Ribbon => _ribbon;

        private ActorInstance Actor =>
            _ribbon == null || _ribbon.ActorGuid == 0 ? null : Actors.Get(_ribbon.ActorGuid);

        public override bool CanFocus =>
            _ribbon != null && _ribbon.gameObject.activeInHierarchy && Actor != null;

        public override string Label => Actors.Name(Actor);

        public override string Value => Actors.StatusLine(Actor);

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate,
                () => EventInspectActor.Trigger(_ribbon.ActorGuid));
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            foreach (var line in TooltipReader.Lines(_ribbon.gameObject)) {
                yield return line;
            }
        }
    }
}
