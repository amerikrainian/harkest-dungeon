using System.Collections.Generic;
using Assets.Code.Item;
using Assets.Code.UI.Banter;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.EventSystems;

namespace DD2A11y.Elements {
    /// <summary>
    /// The driving HUD's stagecoach pet icon, shown while a pet rides the coach's pet slot:
    /// its name (the player's own if renamed, else the item's title), with the icon's hover
    /// description as buffer lines - a stress-barking pet carries no hover text in the game
    /// either, its barks are its voice. Enter is the icon's own click, which pets the pet.
    /// </summary>
    public sealed class DrivingPetElement : UIElement {
        private readonly DrivingPetIconBhv _icon;

        public DrivingPetElement(DrivingPetIconBhv icon) {
            _icon = icon;
        }

        public override bool CanFocus
            => _icon != null && _icon.gameObject.activeInHierarchy && Label != null;

        public override string Label {
            get {
                var pet = RunStatus.Pet();
                if (pet == null) {
                    return null;
                }
                string name = pet.GetName();
                return string.IsNullOrWhiteSpace(name)
                    ? ItemDescription.GetTitle(pet.GetItemDefinition()) : name;
            }
        }

        public override string Role => S.RoleButton;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () =>
                ExecuteEvents.Execute(_icon.gameObject, new BaseEventData(EventSystem.current),
                    ExecuteEvents.submitHandler));
        }

        protected override IEnumerable<string> GetDetailLines() => TooltipReader.Lines(_icon.gameObject);
    }
}
