using System.Collections.Generic;
using Assets.Code.Inn.Presentation;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// The inn's wanted poster (Confessions: the Bounty Hunter's hire offer), a selectable
    /// hanging in the inn scene off the station bar. It carries no text of its own, so it
    /// reads by the hire dialog's title; Enter is the poster's own submit, which opens that
    /// dialog. The game locks the poster while a dialog stands, spoken as unavailable.
    /// </summary>
    public sealed class HunterPosterElement : UIElement {
        private readonly HunterPosterBhv _poster;

        public HunterPosterElement(HunterPosterBhv poster) {
            _poster = poster;
        }

        public override bool CanFocus => _poster != null && _poster.gameObject.activeInHierarchy;

        public override string Label => S.InnHunterPoster(
            GameLoc.TryGet("hunter_hire_dialog_title_label") ?? GameLoc.TryGet("bounty_hunter"));

        public override string Role => S.RoleButton;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                var selectable = _poster.GetComponent<Selectable>();
                if (selectable != null && !selectable.interactable) {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                    return;
                }
                ExecuteEvents.Execute(_poster.gameObject, new BaseEventData(EventSystem.current),
                    ExecuteEvents.submitHandler);
            });
        }
    }
}
