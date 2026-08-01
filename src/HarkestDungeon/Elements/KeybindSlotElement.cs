using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Options;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One key slot of a rebindable command row, labeled with the game's own Key 1/Key 2 column
    /// header. The value is the bound key's display string from the row's data context - the
    /// model layer, since the TMP labels apply a frame late. Enter starts the game's interactive
    /// rebind, whose "Press Key to Set" prompt reads back as the activation feedback (the next
    /// key pressed becomes the binding, Escape keeps the old one); Shift+Enter clears the slot
    /// and reads the cleared state.
    /// </summary>
    public sealed class KeybindSlotElement : UIElement {
        private static readonly AccessTools.FieldRef<RebindInputActionBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<RebindInputActionBhv, DataContextBhv>("m_dataContextBhv");

        private readonly RebindInputActionBhv _row;
        private readonly int _slot;
        private readonly Action<string, bool> _speak;

        public KeybindSlotElement(RebindInputActionBhv row, int slot, Action<string, bool> speak) {
            _row = row;
            _slot = slot;
            _speak = speak;
        }

        /// <summary>The row's command name ("Inventory"), for the row container's label.</summary>
        internal static string CommandLabel(RebindInputActionBhv row) {
            var context = ContextField(row);
            return context != null ? context.GetStringValue("key_label") : null;
        }

        public override bool CanFocus => _row != null && _row.gameObject.activeInHierarchy;

        public override string Label => GameLoc.TryGet(
            _slot == 0 ? "options_key_bindings_header_key_1" : "options_key_bindings_header_key_2");

        public override string Role => S.RoleButton;

        public override string Value {
            get {
                var context = ContextField(_row);
                return context != null
                    ? context.GetStringValue(_slot == 0 ? "first_key_label" : "second_key_label")
                    : null;
            }
        }

        // Starting the rebind swaps the slot's value for the game's "Press Key to Set" prompt
        // (set synchronously into the data context) - the read-back is the listening cue.
        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => _row.OnInputActionSelected(_slot));
            yield return new ElementAction("discard", () => {
                _row.ClearBinding(_slot);
                _speak(GetValueText(), true);
            });
        }
    }
}
