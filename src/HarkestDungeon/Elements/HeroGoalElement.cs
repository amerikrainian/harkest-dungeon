using System;
using System.Collections.Generic;
using Assets.Code.Actor;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using UnityEngine;

namespace DD2A11y.Elements {
    /// <summary>
    /// A hero's run goal slot on the driving goals panel and the sheet's Hero Goals section,
    /// composed from the model the way the game's own rows are: the goal's flavour text or
    /// description with its progress count, "complete" leading once met, and the reward the
    /// row's tooltip names on the line (its first line - a candle count, a trinket's title).
    /// The buffer keeps the goal as its head and the whole reward tooltip as its own entries
    /// under it. Everything reads live: the game writes its
    /// row text once per populate and marks completion only by strikethrough and a checkmark,
    /// and the count alone cannot tell - a per-fight skill-use tally reads back at zero once
    /// the battle ends. Skipped while its row is hidden.
    /// </summary>
    public sealed class HeroGoalElement : UIElement {
        private readonly Func<ActorInstance> _hero;
        private readonly GameObject _row;
        private readonly bool _nameHero;

        /// <param name="hero">The hero the slot concerns, resolved live.</param>
        /// <param name="row">The game's row object: gates visibility and carries the reward
        /// tooltip.</param>
        /// <param name="nameHero">Whether the line leads with the hero's name (the panel
        /// shows only a portrait; the sheet already names its hero).</param>
        public HeroGoalElement(Func<ActorInstance> hero, GameObject row, bool nameHero) {
            _hero = hero;
            _row = row;
            _nameHero = nameHero;
        }

        public override bool CanFocus => Label != null;

        public override string Status => Actors.GoalStatus(_hero());

        public override string Label {
            get {
                if (!_row.activeInHierarchy) {
                    return null;
                }
                var hero = _hero();
                return SpokenLine.Join(_nameHero ? Actors.Name(hero) : null, Actors.GoalText(hero));
            }
        }

        public override string Value {
            get {
                foreach (var line in TooltipReader.Lines(_row)) {
                    return line;
                }
                return null;
            }
        }

        public override string GetBufferHeadText() => SpokenLine.Join(Status, Label);

        // The reward stays out of the head's fold set so its tooltip reviews as separate lines.
        protected override IEnumerable<string> GetBufferHeadParts() {
            yield return Label;
        }

        protected override IEnumerable<string> GetDetailLines() => TooltipReader.Lines(_row);
    }
}
