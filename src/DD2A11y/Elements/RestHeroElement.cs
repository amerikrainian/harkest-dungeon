using Assets.Code.Actor;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One hero on the inn's rest strip: name with HP and stress read from the live actor, the
    /// slot's own status tooltip as buffer lines, and Enter through the game's own submit (which
    /// applies a selected rest item or opens the hero's own interactions). Hides while the slot
    /// has no hero.
    /// </summary>
    public sealed class RestHeroElement : SelectableElement {
        private readonly RestItemSlotBhv _slot;

        public RestHeroElement(RestItemSlotBhv slot, Selectable selectable)
            : base(selectable) {
            _slot = slot;
        }

        private ActorInstance Actor => _slot != null && _slot.IsActive ? Actors.Get(_slot.ActorGuid) : null;

        public override bool CanFocus => base.CanFocus && Actor != null;

        public override string Label => Actors.Name(Actor);

        public override string Role => null;

        public override string Value {
            get {
                var actor = Actor;
                if (actor == null) {
                    return null;
                }
                string hpFormat = GameLoc.TryGet("status_bar_health");
                string hp = hpFormat == null ? (int)actor.DisplayedHp + "/" + (int)actor.DisplayedHpMax
                    : string.Format(hpFormat, (int)actor.DisplayedHp, (int)actor.DisplayedHpMax);
                string stressFormat = GameLoc.TryGet("status_bar_stress");
                string stress = stressFormat == null ? null
                    : string.Format(stressFormat, (int)actor.Stress, (int)actor.StressMax);
                return SpokenLine.Join(hp, stress);
            }
        }
    }
}
