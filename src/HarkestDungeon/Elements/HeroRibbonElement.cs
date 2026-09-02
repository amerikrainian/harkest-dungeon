using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Actor.Events;
using Assets.Code.UI.Banter;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One hero on a ribbon strip (the road's party bar, the lair advance dialog): name with
    /// rank (the marching order the strip draws, combat's own caption), HP, and stress from
    /// the live actor, every ribbon tooltip (the status bar's effects, diseases) as buffer
    /// lines. Enter is the ribbon's own right-click inspect - the hero's character sheet; on
    /// the road, Space grabs the hero for a marching-order move, which the driving screen
    /// routes.
    /// </summary>
    public sealed class HeroRibbonElement : UIElement {
        private readonly HeroRibbonBhv _ribbon;

        public HeroRibbonElement(HeroRibbonBhv ribbon) {
            _ribbon = ribbon;
        }

        public HeroRibbonBhv Ribbon => _ribbon;

        private ActorInstance Actor =>
            _ribbon == null || _ribbon.ActorGuid == 0 ? null : Actors.Get(_ribbon.ActorGuid);

        public override bool CanFocus =>
            _ribbon != null && _ribbon.gameObject.activeInHierarchy && Actor != null;

        public override string Label => Actors.Name(Actor);

        // The ribbon's notification dot (unviewed character-sheet notifications) reads as the
        // shared "New" marker, cleared the same way the game clears it - by viewing the sheet.
        public override string Value {
            get {
                var actor = Actor;
                return SpokenLine.Join(Actors.RankText(actor), Actors.StatusLine(actor),
                    actor != null && !actor.ViewedCharacterSheetNotifications ? S.TutorialNew : null);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate,
                () => EventInspectActor.Trigger(_ribbon.ActorGuid));
        }

        protected override IEnumerable<string> GetDetailLines() {
            foreach (var line in TooltipReader.Lines(_ribbon.gameObject)) {
                yield return line;
            }
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => bufferKey == Core.Buffers.BufferKeys.Hero
                ? HeroStatus.Lines(Actor) : base.GetSideBufferLines(bufferKey);
    }
}
