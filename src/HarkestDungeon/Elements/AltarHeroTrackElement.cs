using System.Collections.Generic;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A hero's icon button heading the hero's milestone row on the altar's Living City
    /// panel: the game's own hero name, with the track's spent/total candles. Enter is the
    /// game's own icon click - one candle into the track, or the store dialog on a DLC hero.
    /// A hero locked behind their quest reads "unavailable" from the game-disabled button,
    /// with the game's own lock caption in the buffer.
    /// </summary>
    public sealed class AltarHeroTrackElement : AltarTrackElement {
        private readonly AltarClassHeroBhv _hero;

        public AltarHeroTrackElement(AltarClassHeroBhv hero, Selectable selectable)
            : base(hero, selectable, () => HeroName(hero)) {
            _hero = hero;
        }

        /// <summary>The game's own name binding; a DLC row leaves it empty and captions the
        /// lock instead. Also the row container's label, so the dedupe folds the two.</summary>
        internal static string HeroName(AltarClassHeroBhv hero) {
            var context = ContextField(hero);
            string name = context.GetStringValue("actor_name");
            return string.IsNullOrEmpty(name) ? context.GetStringValue("locked_label") : name;
        }

        protected override void Spend() {
            string before = Total;
            _hero.OnIconClick();
            if (string.IsNullOrEmpty(ContextField(_hero).GetStringValue("actor_name"))) {
                return; // a DLC row's click opens the store dialog, which announces itself
            }
            SpeakSpendResult(before);
        }

        public override IEnumerable<string> GetBufferLines() {
            foreach (var line in base.GetBufferLines()) {
                yield return line;
            }
            // The quest lock's caption; a DLC row already carries it as the label.
            if (Selectable != null && !Selectable.interactable) {
                string locked = ContextField(_hero).GetStringValue("locked_label");
                if (!string.IsNullOrEmpty(locked) && locked != Label) {
                    yield return locked;
                }
            }
        }
    }
}
