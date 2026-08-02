using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens.Options {
    /// <summary>
    /// The mod keys tab: one <see cref="KeyRebindElement"/> per registered mod command, in
    /// registration order. A listen left dangling when the tab goes away is aborted silently -
    /// the switch or close speaks for itself.
    /// </summary>
    public sealed class ModKeysTab : ModTab {
        private readonly InputManager _input;
        private readonly ModKeymap _keymap;
        private readonly ModRebind _rebind;
        private readonly System.Action<string, bool> _speak;

        public ModKeysTab(InputManager input, ModKeymap keymap, ModRebind rebind,
                          System.Action<string, bool> speak) {
            _input = input;
            _keymap = keymap;
            _rebind = rebind;
            _speak = speak;
        }

        public override string Name => S.TabModKeys;

        public override void Populate(Container items) {
            foreach (var action in _input.Actions) {
                items.Add(new KeyRebindElement(action, _keymap, _rebind, _speak));
            }
        }

        public override void OnHidden() => _rebind.Abort();
    }
}
