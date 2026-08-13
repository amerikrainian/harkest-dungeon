using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Elements {
    /// <summary>
    /// One cosmetic swatch on the hero sheet's Cosmetics tab (a palette, weapon kit, or hero
    /// skin). The swatch itself is a color patch or a two-letter code; the spoken name is the
    /// game's own cosmetic name, the same string its tooltip carries. "Selected" marks the
    /// applied choice (read live, so applying one updates every swatch), a locked skin refuses
    /// with "unavailable" (its unlock hint is the tooltip in the buffer), and an unviewed
    /// cosmetic carries the game's notification marker. Enter applies through the button's own
    /// submit logic.
    /// </summary>
    public sealed class CosmeticButtonElement : SelectableElement {
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticButtonBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticButtonBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticButtonBhv, GameObject> NotificationField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticButtonBhv, GameObject>("m_notificationIcon");

        private readonly CharacterSheetCosmeticButtonBhv _button;
        private readonly Func<int, string> _nameOf;

        public CosmeticButtonElement(CharacterSheetCosmeticButtonBhv button, Func<int, string> nameOf)
            : base(button) {
            _button = button;
            _nameOf = nameOf;
        }

        public override string Label
            => _button.Index < 0 ? GameLoc.TryGet("default_label") : _nameOf(_button.Index);

        public override string Status {
            get {
                var notification = NotificationField(_button);
                return SpokenLine.Join(
                    _button.IsToggleOn ? S.StatusSelected : null,
                    Locked ? S.StatusUnavailable : base.Status,
                    notification != null && notification.activeSelf ? S.TutorialNew : null);
            }
        }

        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            // The game's own submit flips the selected border before its locked gate refuses,
            // leaving a locked skin looking applied; refusing here keeps the state honest.
            if (Locked) {
                yield return new ElementAction(ActionIds.Activate,
                    () => SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true));
                yield break;
            }
            foreach (var action in base.GetActions()) {
                yield return action;
            }
        }

        private bool Locked {
            get {
                var context = ContextField(_button);
                return context != null && context.GetBoolValue("locked");
            }
        }
    }
}
