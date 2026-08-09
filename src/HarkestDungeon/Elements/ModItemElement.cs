using System;
using System.Collections.Generic;
using Assets.Code.UI;
using DD2A11y.Core.Nav;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// One mod in the mod panel's load-order list: enabled state first, the mod's name, then
    /// its version. Enter flips the mod's own enable toggle (the game's all-toggles follow);
    /// Space grabs the row and a second Space on another row drops it there - the game's own
    /// reorder submit, run through the screen so the landing speaks. The buffer carries the
    /// short and expanded descriptions and any validation error the row shows.
    /// </summary>
    public sealed class ModItemElement : UIElement {
        private static readonly AccessTools.FieldRef<ModItemBhv, TextMeshProUGUI> ErrorTextField =
            AccessTools.FieldRefAccess<ModItemBhv, TextMeshProUGUI>("m_errorText");

        private readonly ModItemBhv _item;
        private readonly Action<ModItemElement> _grab;

        public ModItemElement(ModItemBhv item, Action<ModItemElement> grab) {
            _item = item;
            _grab = grab;
        }

        public ModItemBhv Item => _item;

        public override bool CanFocus => _item != null && _item.gameObject.activeInHierarchy;

        public override string Status => _item.IsOn ? S.StatusOn : S.StatusOff;

        public override string Label => _item.ModName;

        public override string Role => S.RoleToggle;

        public override string Value => _item.data?.metaData;

        public override bool ReannounceOnActivate => true;

        // The flip's feedback is the new state alone; the version never changes with it.
        public override string GetValueText() => Status;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, Toggle);
            yield return new ElementAction("grab", () => _grab(this));
        }

        private void Toggle() {
            if (_item.IsOn) {
                _item.Disable();
            } else {
                _item.Enable();
            }
        }

        protected override IEnumerable<string> GetDetailLines() {
            var data = _item.data;
            if (data != null) {
                if (!string.IsNullOrWhiteSpace(data.description)) {
                    yield return data.description;
                }
                if (!string.IsNullOrWhiteSpace(data.expandedDescription)
                    && data.expandedDescription != data.description) {
                    yield return data.expandedDescription;
                }
            }
            var error = ErrorTextField(_item);
            if (error != null && !string.IsNullOrWhiteSpace(error.text)) {
                yield return error.text;
            }
        }
    }
}
