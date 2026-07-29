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
        private int _builtSignature;

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
            Populate(panel);
            return _root;
        }

        // The option rows spawn a beat after the stack entry appears; a too-early build
        // would otherwise stay empty forever (observed live entering on the entry frame).
        public override bool OnUpdate(object target) {
            var panel = (AltarOptionsSubscreenBhv)target;
            if (Signature(panel) != _builtSignature) {
                bool wasEmpty = _root.Children.Count == 0;
                Populate(panel);
                return wasEmpty; // a late first fill re-announces so entry is never dead air
            }
            return false;
        }

        private void Populate(AltarOptionsSubscreenBhv panel) {
            _root.Clear();
            foreach (var item in panel.GetComponentsInChildren<OptionsItemBhv>(includeInactive: false)) {
                var element = OptionsItemElement.TryCreate(item);
                if (element != null) {
                    _root.Add(element);
                }
            }
            _builtSignature = Signature(panel);
        }

        private static int Signature(AltarOptionsSubscreenBhv panel) {
            int signature = 17;
            foreach (var item in panel.GetComponentsInChildren<OptionsItemBhv>(includeInactive: false)) {
                signature = signature * 31 + item.GetInstanceID();
            }
            return signature;
        }
    }
}
