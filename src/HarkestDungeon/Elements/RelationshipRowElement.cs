using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One partner row on the hero sheet's Relationships tab: the partner's name, then the
    /// affinity readout the sighted banner shows - the band word with the pip meter
    /// ("Neutral, 9/20") while affinity is still building, or the formed relationship's name
    /// (plus its remaining days in Kingdoms) once one exists - all read live from the row's
    /// own data bindings. The affinity tooltip (band description, formation-chance breakdown)
    /// is the buffer. Enter is the game's own click: it moves the sheet to this partner, so
    /// activation names the hero the sheet then shows.
    /// </summary>
    public sealed class RelationshipRowElement : SelectableElement {
        private readonly CharacterSheetRelationshipActorUiBhv _row;

        public RelationshipRowElement(CharacterSheetRelationshipActorUiBhv row, Selectable selectable)
            : base(selectable) {
            _row = row;
        }

        public override string Value {
            get {
                var context = _row == null ? null : _row.GetComponent<DataContextBhv>();
                if (context == null) {
                    return null;
                }
                string band = GameLoc.TryGet(context.GetStringValue("affinity_name"));
                if (context.GetBoolValue("affinity_leaning_ticks_visisble")) {
                    return SpokenLine.Join(band, context.GetStringValue("pip_value"));
                }
                // A formed relationship replaces the meter; its countdown exists in Kingdoms
                // only (the game's own gate on the duration widget).
                string duration = Singleton<GameTypeMgr>.Instance.CurrentGameType == GameType.KINGDOM
                    ? context.GetStringValue("relationship_duration") : null;
                return SpokenLine.Join(band, duration);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (Selectable == null || !Selectable.interactable) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, () => {
                Submit();
                // The click moved the sheet to this partner; the rebuild announces a row of
                // the new sheet, not its owner, so name the hero the sheet now shows.
                var guid = SingletonMonoBehaviour<CommonUiBhv>.Instance.ActiveCharacterSheetActorGuid;
                var actor = Actors.Get(guid);
                if (actor != null) {
                    SpeechPipeline.Instance?.Speak(actor.ActorName);
                }
            });
        }
    }
}
