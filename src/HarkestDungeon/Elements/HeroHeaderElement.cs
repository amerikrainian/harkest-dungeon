using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.UI.Tooltips;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// The hero sheet's header: the hero's name, with class and path as the value. Left/Right
    /// (the advertised increase/decrease actions) page to the next/previous hero through the
    /// sheet's own switching, and read the landed hero in full. The hero path's full description
    /// (the seal tooltip) lives in the buffer.
    /// </summary>
    public sealed class HeroHeaderElement : UIElement {
        private static readonly AccessTools.FieldRef<CharacterSheetUiBhv, TextTooltipBhv> SealTooltipField =
            AccessTools.FieldRefAccess<CharacterSheetUiBhv, TextTooltipBhv>("m_heroSealTooltip");

        private readonly CharacterSheetUiBhv _sheet;

        public HeroHeaderElement(CharacterSheetUiBhv sheet) {
            _sheet = sheet;
        }

        private ActorInstance Actor => Actors.Get(_sheet.ActorGuid);

        public override string Label => Actor?.ActorName;

        public override string Role => S.RoleHero;

        public override string Value {
            get {
                var actor = Actor;
                if (actor == null) {
                    return null;
                }
                string className = GameLoc.TryGet(actor.ActorDataId);
                string pathName = ActorPathDescription.GetNameString(
                    actor.ActorDataPath, actor.ActorDataClass.m_LocalizationGender, addColor: false);
                return SpokenLine.Join(className, pathName);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Increase, _sheet.HandleNextActorButton);
            yield return new ElementAction(ActionIds.Decrease, _sheet.HandlePreviousActorButton);
        }

        // A hero switch reads the landed hero in full; with a single hero the switch lands on the
        // same one, which is still the right thing to read (this control wraps, it has no ends).
        public override string GetAdjustText(string actionId, bool changed) => GetFocusText();

        protected override IEnumerable<string> GetDetailLines() {
            foreach (var line in ClassDescription.Lines(Actor?.ActorDataId)) {
                yield return line;
            }
            foreach (var line in TooltipReader.LinesOf(SealTooltipField(_sheet))) {
                yield return line;
            }
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => bufferKey == Core.Buffers.BufferKeys.Hero
                ? HeroStatus.Lines(_sheet.ActorGuid) : base.GetSideBufferLines(bufferKey);
    }
}
