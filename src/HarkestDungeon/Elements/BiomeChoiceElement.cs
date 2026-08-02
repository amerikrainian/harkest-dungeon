using Assets.Code.Data;
using Assets.Code.Inn;
using Assets.Code.Inn.Presentation;
using Assets.Code.Utils;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One route option on the inn's Select Route screen: the destination region's own name,
    /// "selected" when it is the chosen route, and the goal/modifier/reward tooltips as
    /// buffer lines. Enter is the game's own submit, which marks this route as the chosen
    /// one (departure happens later, through the inn's own depart flow).
    /// </summary>
    public sealed class BiomeChoiceElement : SelectableElement {
        private readonly int _index;

        public BiomeChoiceElement(BiomeChoiceBhv choice, Selectable selectable, int index)
            : base(selectable, () => ChoiceName(choice)) {
            _index = index;
        }

        private static string ChoiceName(BiomeChoiceBhv choice) {
            var context = choice == null ? null : choice.GetComponent<DataContextBhv>();
            string stored = context == null ? null : context.GetStringValue("biome_label");
            // The binding usually stores the loc key; one branch stores the resolved name.
            return GameLoc.TryGet(stored) ?? stored;
        }

        public override string Status
            => Singleton<InnBhv>.Instance.GetSelectedBiomeChoiceIndex() == _index ? S.StatusSelected : null;

        public override bool ReannounceOnActivate => true;
    }
}
