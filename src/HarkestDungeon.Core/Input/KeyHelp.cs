using System;
using System.Collections.Generic;

namespace DD2A11y.Core.Input {
    /// <summary>
    /// The keys that would do something right now, for the active screen and the focused
    /// element: the context-sensitive key help. Nothing is declared for it. An action is listed
    /// when a key of it is live (its category active, its chord not shadowed) and something
    /// answers it: the dispatcher, asked without acting (an arrow with somewhere to go, Enter on
    /// an element that answers it, Escape where the screen closes), else the action's own
    /// handler, unless its <see cref="InputAction.Available"/> says it would not act (a glance is
    /// not listed where its fact is not on screen).
    /// <para>In order, the most particular first: the keys of the screen's own categories (the
    /// combat glances, the kingdom map's points of interest); the UI keys that say when they
    /// apply (inspect, discard, grab); the keys the screen and the navigator answer; the UI keys
    /// that always act (the buffers); the global ones. Collected while the screen still has the
    /// focus: once the help is up, the focused element is the help's own.</para>
    /// </summary>
    public static class KeyHelp {
        /// <param name="dispatchDryRun">What the dispatcher would do with a press of the action:
        /// true = consume it to effect, false = consume it to no effect, null = pass it on to the
        /// action's handler.</param>
        /// <param name="warn">Where a failing check is reported; that action is left out.</param>
        /// <param name="skip">Actions never listed (the help's own key).</param>
        public static List<InputAction> Collect(IEnumerable<InputAction> actions, Func<InputAction, bool> isLive,
                                                Func<InputAction, bool?> dispatchDryRun, Action<string> warn,
                                                Func<InputAction, bool>? skip = null) {
            var screenKeys = new List<InputAction>();
            var contextKeys = new List<InputAction>();
            var dispatchedKeys = new List<InputAction>();
            var generalKeys = new List<InputAction>();
            var globalKeys = new List<InputAction>();

            foreach (var action in actions) {
                if ((skip != null && skip(action)) || !isLive(action)) {
                    continue;
                }
                try {
                    bool? dispatched = dispatchDryRun(action);
                    if (dispatched == true) {
                        dispatchedKeys.Add(action);
                        continue;
                    }
                    if (dispatched == false || !action.HasHandler
                        || (action.Available != null && !action.Available())) {
                        continue;
                    }
                    var list = action.Category == InputCategory.Global ? globalKeys
                        : action.Category != InputCategory.UI ? screenKeys
                        : action.Available != null ? contextKeys
                        : generalKeys;
                    list.Add(action);
                } catch (Exception e) {
                    warn("key help: checking " + action.Key + " failed: " + e.Message);
                }
            }

            var all = new List<InputAction>(screenKeys);
            all.AddRange(contextKeys);
            all.AddRange(dispatchedKeys);
            all.AddRange(generalKeys);
            all.AddRange(globalKeys);
            return all;
        }

        /// <summary>The action's keys as the help reads them ("Enter, Numpad Enter"), leaving out
        /// any binding <paramref name="include"/> refuses (a pad combo with no pad connected) -
        /// unless that would leave none.</summary>
        public static string KeysOf(InputAction action, Func<InputBinding, bool>? include = null) {
            var names = new List<string>();
            foreach (var binding in action.Bindings) {
                if (include == null || include(binding)) {
                    names.Add(binding.DisplayName);
                }
            }
            if (names.Count == 0) {
                foreach (var binding in action.Bindings) {
                    names.Add(binding.DisplayName);
                }
            }
            return string.Join(", ", names);
        }
    }
}
