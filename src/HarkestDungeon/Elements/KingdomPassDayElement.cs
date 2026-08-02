using System.Collections.Generic;
using Assets.Code.Audio;
using Assets.Code.Kingdom;
using Assets.Code.UI.Kingdom;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using FMODUnity;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// The kingdom map's pass-day button. The game commits it on a one-second pointer hold
    /// (UIPointerDownBhv driving SetPassHeld - the Button's onClick is unwired), so Enter runs
    /// the game's own OnPassDay directly behind the hold's own gates, with the hold's confirm
    /// sound; a refused press answers "unavailable". The day transition announces itself.
    /// </summary>
    public sealed class KingdomPassDayElement : SelectableElement {
        private static readonly System.Func<KingdomUiBhv, bool> CanPassDayMethod =
            AccessTools.MethodDelegate<System.Func<KingdomUiBhv, bool>>(
                AccessTools.Method(typeof(KingdomUiBhv), "CanPassDay"));
        private static readonly AccessTools.FieldRef<KingdomUiBhv, EventReference> PassDaySfxField =
            AccessTools.FieldRefAccess<KingdomUiBhv, EventReference>("m_passDayClickSfx");

        private readonly KingdomUiBhv _ui;

        public KingdomPassDayElement(KingdomUiBhv ui, Selectable selectable) : base(selectable) {
            _ui = ui;
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                // The hold's own gate, plus OnPassDay's internal conditions - without them a
                // refused press would no-op silently.
                if (!CanPassDayMethod(_ui) || _ui.InAltView
                    || SingletonMonoBehaviour<KingdomBhv>.Instance.KingdomDaySystem.GetDayState()
                        != KingdomDayState.WAIT_ON_PLAYER) {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                    return;
                }
                _ui.OnPassDay();
                SingletonMonoBehaviour<AudioMgr>.Instance.Play(PassDaySfxField(_ui));
            });
        }
    }
}
