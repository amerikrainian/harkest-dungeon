using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// The Mastery Trainer's hero header: the shown hero's name with the mastery points
    /// remaining; Left/Right page through the party via the trainer's own arrow buttons.
    /// </summary>
    public sealed class MasteryHeroElement : UIElement {
        private readonly Func<string> _hero;
        private readonly Func<int> _points;
        private readonly Button _previous;
        private readonly Button _next;

        public MasteryHeroElement(Func<string> hero, Func<int> points, Button previous, Button next) {
            _hero = hero;
            _points = points;
            _previous = previous;
            _next = next;
        }

        public override string Label => _hero();

        public override string Role => S.RoleHero;

        public override string Value => S.MasteryPoints(_points());

        public override IEnumerable<ElementAction> GetActions() {
            if (_previous != null) {
                yield return new ElementAction(ActionIds.Decrease, () => Press(_previous));
            }
            if (_next != null) {
                yield return new ElementAction(ActionIds.Increase, () => Press(_next));
            }
        }

        // Paging changes the label, not the value, so the adjust feedback is the new hero.
        public override string GetAdjustText(string actionId, bool changed) => GetFocusText();

        private static void Press(Button button) {
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
        }
    }
}
