using Assets.Code.Inn.Presentation;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn's Select Route screen (a <c>SubScreenBiomeChoiceBhv</c> stack entry), named by
    /// the inn header's station title: one element per offered route - the destination
    /// region's own name, "selected" on the chosen one, goal and modifier and reward detail
    /// in the buffer - with Enter marking the choice through the game's own submit. An inn
    /// that offers no choices reads "empty". Escape closes back to the inn.
    /// </summary>
    public sealed class RouteSelectScreen : GameScreen {
        private SubScreenBiomeChoiceBhv _panel;
        private Container _root;
        private int _builtCount;

        public override string Name => InnStations.Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<SubScreenBiomeChoiceBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (SubScreenBiomeChoiceBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: panel.CloseSubscreen);
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (SubScreenBiomeChoiceBhv)target;
            if (CountChoices(panel) != _builtCount) {
                _root.Clear();
                Populate(panel);
            }
            return false;
        }

        private void Populate(SubScreenBiomeChoiceBhv panel) {
            int index = 0;
            foreach (var choice in Choices(panel)) {
                var selectable = choice.GetComponent<Selectable>();
                if (selectable != null) {
                    _root.Add(new BiomeChoiceElement(choice, selectable, index));
                }
                index++;
            }
            if (_root.IsEmptyContainer) {
                _root.Add(new StaticTextElement(() => S.PanelEmpty));
            }
            _builtCount = CountChoices(panel);
        }

        private static int CountChoices(SubScreenBiomeChoiceBhv panel) {
            int count = 0;
            foreach (var choice in Choices(panel)) {
                count++;
            }
            return count;
        }

        // The choice widgets spawn under the panel's own container, in offer order.
        private static System.Collections.Generic.IEnumerable<BiomeChoiceBhv> Choices(SubScreenBiomeChoiceBhv panel) {
            foreach (var choice in panel.GetComponentsInChildren<BiomeChoiceBhv>(includeInactive: false)) {
                yield return choice;
            }
        }
    }
}
