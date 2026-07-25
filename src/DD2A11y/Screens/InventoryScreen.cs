using System;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The standalone player inventory - the "Inventory (I)" screen the game opens on the
    /// road, at the crossroads, and from the loot screen (the inn reads the same panel inline
    /// through its own hub, which outranks this by registration order). Nothing but the shared
    /// bag panel; Escape drops an armed grab first, else runs the game's own close.
    /// </summary>
    public sealed class InventoryScreen : GameScreen {
        private readonly InventoryPanel _panel;
        private InventoryUiBhv _inventory;
        private Container _root;
        private bool _awaitingLabel;

        public InventoryScreen(Action<string, bool> speak, TraditionalNavigator navigator) {
            _panel = new InventoryPanel(speak, navigator);
        }

        /// <summary>The grab key (Space / Shift+Space), routed here while this screen stands.</summary>
        public void ToggleGrab(UIElement current, bool takeOne) => _panel.ToggleGrab(current, takeOne);

        public override string Name => S.ScreenInventory;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _inventory = top == null ? null : top.GetComponent<InventoryUiBhv>();
            return _inventory;
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList, back: Back);
            _panel.BuildInto(_root, (InventoryUiBhv)target);
            // Opened mid-animation the filter tab has no title yet and the landing reads a bare
            // "tab"; the arrival of its label requests the one re-announce.
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
            return _root;
        }

        public override bool OnUpdate(object target) {
            _panel.Update();
            return PauseScreen.LabelArrived(_root, ref _awaitingLabel);
        }

        private void Back() {
            if (_panel.GrabArmed) {
                _panel.CancelGrab();
                return;
            }
            SingletonMonoBehaviour<CommonUiBhv>.Instance.HidePlayerInventory();
        }
    }
}
