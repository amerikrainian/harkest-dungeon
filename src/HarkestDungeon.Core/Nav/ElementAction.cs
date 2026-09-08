using System;

namespace DD2A11y.Core.Nav {
    /// <summary>
    /// An action a <see cref="UIElement"/> advertises (activate, back, increase, ...). Navigators
    /// discover actions by <see cref="Id"/> and invoke them; they never switch on element type.
    /// Distinct from <c>InputAction</c>, which is a keybinding. An action built with
    /// <see cref="Refusable"/> may decline a press (a locked pick, a cost the wallet cannot
    /// cover); the navigator speaks the refusal, so the element itself stays silent.
    /// </summary>
    public sealed class ElementAction {
        public string Id { get; }
        private readonly Func<bool> _execute;

        public ElementAction(string id, Action execute) {
            Id = id;
            _execute = () => {
                execute();
                return true;
            };
        }

        private ElementAction(string id, Func<bool> execute) {
            Id = id;
            _execute = execute;
        }

        /// <summary>An action whose press the element may refuse: <paramref name="execute"/>
        /// answers false to decline, having changed nothing.</summary>
        public static ElementAction Refusable(string id, Func<bool> execute) => new ElementAction(id, execute);

        /// <summary>Run the action; false when the element refused it.</summary>
        public bool Execute() => _execute();
    }

    /// <summary>What invoking an element's action by id came to.</summary>
    public enum ActionOutcome {
        /// <summary>The element advertises no such action.</summary>
        Missing,
        /// <summary>The element refused the press; the caller speaks the refusal.</summary>
        Refused,
        /// <summary>The action ran.</summary>
        Done,
    }

    /// <summary>Standard action ids navigators map inputs to.</summary>
    public static class ActionIds {
        public const string Activate = "activate"; // primary action (Enter)
        public const string Back = "back";         // screen-level back/close (Escape)
        public const string Increase = "increase"; // arrow-right on a slider/stepper/tab selector
        public const string Decrease = "decrease"; // arrow-left on a slider/stepper/tab selector
    }
}
