using Assets.Code.UI;
using Assets.Code.UI.Options;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's game-options panel (an <c>AltarOptionsSubscreenBhv</c> pushed by a region -
    /// "The Dam"), named by its exit anchor's title: one settings row per altar option
    /// (Enable Retreat, Enable Hoarder Selling, ...). A row the profile has not earned reads
    /// its state plus "unavailable", with the game's unlock requirement in the buffer. Enter
    /// flips a toggle through the game's own submit; the profile saves on close. Escape closes
    /// through the panel's own close flow (a raw stack pop would leave the altar's region
    /// markers disabled).
    /// </summary>
    public sealed class AltarOptionsScreen : GameScreen {
        private AltarOptionsSubscreenBhv _panel;
        private Container _root;

        public override string Name {
            get {
                string title = UiText.ChildLabel(_panel != null ? _panel.gameObject : null, "exit_anchor");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<AltarOptionsSubscreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarOptionsSubscreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: panel.CloseSubscreen);
            foreach (var item in panel.GetComponentsInChildren<OptionsItemBhv>(includeInactive: false)) {
                var element = OptionsItemElement.TryCreate(item);
                if (element != null) {
                    _root.Add(element);
                }
            }
            return _root;
        }
    }
}
