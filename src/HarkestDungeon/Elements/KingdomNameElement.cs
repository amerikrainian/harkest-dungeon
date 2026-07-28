using System.Collections.Generic;
using Assets.Code.Kingdom.UI;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// The kingdom-name entry field in the creation wizard, labeled with the step's own title
    /// string. Enter starts the game's edit flow (clears the field and takes typing); the screen
    /// echoes keystrokes and re-reads the field when the edit ends.
    /// </summary>
    public sealed class KingdomNameElement : UIElement {
        private readonly KingdomSelectNameInputBhv _step;
        private readonly TMP_InputField _field;

        public KingdomNameElement(KingdomSelectNameInputBhv step) {
            _step = step;
            _field = step.GetComponentInChildren<TMP_InputField>(includeInactive: true);
        }

        public TMP_InputField Field => _field;

        public override bool CanFocus => _step != null && _step.gameObject.activeInHierarchy;

        public override string Label => GameLoc.TryGet("kingdom_creation_name_input_label");

        public override string Role => S.RoleEdit;

        public override string Value => _field != null ? _field.text : null;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _step.OnEditNameButtonPressed);
        }
    }
}
