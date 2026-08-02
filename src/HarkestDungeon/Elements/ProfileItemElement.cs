using System;
using System.Collections.Generic;
using Assets.Code.Profile;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// One profile in the select list: the profile's name (or the game's "Create New" caption on
    /// an empty slot), with the active profile marked selected from the profile model. Enter
    /// drives the game's select handler (which creates on an empty slot); Shift+Enter opens the
    /// game's delete confirmation. The row widget is resolved live through the delegate because
    /// the game's refresh recycles every row through a pool.
    /// </summary>
    public sealed class ProfileItemElement : UIElement {
        private static readonly AccessTools.FieldRef<ProfileSelectItemBhv, TMP_InputField> NameField =
            AccessTools.FieldRefAccess<ProfileSelectItemBhv, TMP_InputField>("m_nameInputField");

        private readonly ProfileSelectBhv _profileSelect;
        private readonly Func<ProfileSelectItemBhv> _resolve;

        public ProfileItemElement(ProfileSelectBhv profileSelect, Func<ProfileSelectItemBhv> resolve) {
            _profileSelect = profileSelect;
            _resolve = resolve;
        }

        private ProfileSelectItemBhv Item => _resolve();

        public override bool CanFocus {
            get {
                var item = Item;
                return item != null && item.gameObject.activeInHierarchy;
            }
        }

        public override string Label {
            get {
                var item = Item;
                if (item == null) {
                    return null;
                }
                var field = NameField(item);
                return field != null ? field.text : null;
            }
        }

        public override string Role => S.RoleButton;

        public override string Status {
            get {
                var item = Item;
                if (item == null || item.IsEmpty) {
                    return null;
                }
                var profile = item.GetProfileInstance();
                bool current = profile != null
                    && SingletonMonoBehaviour<ProfileBhv>.Instance.GetCurrentProfileGuid() == profile.ProfileGuid;
                return current ? S.StatusSelected : null;
            }
        }

        // Selecting a profile changes state in place (the read-back "selected" is the feedback);
        // an empty slot opens the creation window, which announces itself.
        public override bool ReannounceOnActivate {
            get {
                var item = Item;
                return item != null && !item.IsEmpty;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            var item = Item;
            if (item == null) {
                yield break;
            }
            yield return new ElementAction(ActionIds.Activate, () => _profileSelect.OnProfileSelected(item));
            if (!item.IsEmpty) {
                yield return new ElementAction("discard", item.OnDeleteProfilePressed);
            }
        }
    }
}
