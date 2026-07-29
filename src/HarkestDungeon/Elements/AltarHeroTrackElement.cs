using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A hero's icon button heading the hero's milestone row on the altar's Living City
    /// panel: the game's own hero name, with the track's spent/total candles. Enter is the
    /// game's own icon click - one candle into the track (partial progress banks toward the
    /// next milestone), or the store dialog on a DLC hero - and reads back the moved total,
    /// or "unavailable" when the spend no-ops (no candles, track full). A hero locked behind
    /// their quest reads "unavailable" from the game-disabled button, with the game's own
    /// lock caption in the buffer.
    /// </summary>
    public sealed class AltarHeroTrackElement : SelectableElement {
        private static readonly AccessTools.FieldRef<AltarProgressTrackBaseBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<AltarProgressTrackBaseBhv, DataContextBhv>("m_dataContextBhv");

        private readonly AltarClassHeroBhv _hero;

        public AltarHeroTrackElement(AltarClassHeroBhv hero, Selectable selectable)
            : base(selectable, () => HeroName(hero)) {
            _hero = hero;
        }

        /// <summary>The game's own name binding; a DLC row leaves it empty and captions the
        /// lock instead. Also the row container's label, so the dedupe folds the two.</summary>
        internal static string HeroName(AltarClassHeroBhv hero) {
            var context = ContextField(hero);
            string name = context.GetStringValue("actor_name");
            return string.IsNullOrEmpty(name) ? context.GetStringValue("locked_label") : name;
        }

        private string Total => ContextField(_hero).GetStringValue("track_total_spent");

        public override string Value {
            get {
                string total = Total;
                return Selectable != null && !Selectable.interactable
                    ? SpokenLine.Join(total, S.StatusUnavailable)
                    : total;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.interactable) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, () => {
                string before = Total;
                _hero.OnIconClick();
                if (string.IsNullOrEmpty(ContextField(_hero).GetStringValue("actor_name"))) {
                    return; // a DLC row's click opens the store dialog, which announces itself
                }
                string after = Total;
                SpeechPipeline.Instance?.Speak(after != before ? after : S.StatusUnavailable,
                    interrupt: true);
            });
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
