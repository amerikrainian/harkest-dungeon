using System.Collections.Generic;
using Assets.Code.Item;
using Assets.Code.UI.Items;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Elements {
    /// <summary>
    /// One collected item in the altar's Recollection gallery: the item's own title, "New"
    /// while the game shows its unviewed notification marker (the game clears the marker for
    /// the next visit as it lists the item), and the full item tooltip in the buffer. The
    /// gallery is browse-only - the sighted widget is uninteractable too - so Enter does
    /// nothing.
    /// </summary>
    public sealed class AltarCollectionItemElement : UIElement {
        private static readonly AccessTools.FieldRef<UninteractableRewardItemBhv, ItemDefinition> DefinitionField =
            AccessTools.FieldRefAccess<UninteractableRewardItemBhv, ItemDefinition>("m_itemDefinition");
        private static readonly AccessTools.FieldRef<UninteractableRewardItemBhv, GameObject> NotificationField =
            AccessTools.FieldRefAccess<UninteractableRewardItemBhv, GameObject>("m_notificationIcon");

        private readonly UninteractableRewardItemBhv _item;

        public AltarCollectionItemElement(UninteractableRewardItemBhv item) {
            _item = item;
        }

        public override bool CanFocus => _item != null && _item.gameObject.activeInHierarchy;

        public override string Label {
            get {
                var definition = DefinitionField(_item);
                return definition == null ? null : ItemDescription.GetTitle(definition);
            }
        }

        public override string Value {
            get {
                var notification = NotificationField(_item);
                return notification != null && notification.activeSelf ? S.TutorialNew : null;
            }
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string label = Label;
            foreach (var line in TooltipReader.Lines(_item.gameObject)) {
                if (line != label) {
                    yield return line;
                }
            }
        }
    }
}
