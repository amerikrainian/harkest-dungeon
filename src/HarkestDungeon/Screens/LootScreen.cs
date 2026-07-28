using Assets.Code.UI.Controllers;
using Assets.Code.UI.Items;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The loot screen (a battle's Victory rewards, a road cache): its description line, then
    /// each reward item - the item's own title and stack size, its full tooltip in the buffer,
    /// Enter taking it through the game's own transfer - then Take All / Leave Items and the
    /// utility buttons. Escape runs the game's own close flow, including its leave-items
    /// confirmation dialog when rewards remain.
    /// </summary>
    public sealed class LootScreen : GameScreen {
        private static readonly AccessTools.FieldRef<LootUiControllerBhv, LootInventoryItemContainerBhv> ItemContainerField =
            AccessTools.FieldRefAccess<LootUiControllerBhv, LootInventoryItemContainerBhv>("m_itemContainerBhv");

        private LootUiControllerBhv _loot;
        private Container _root;
        private Container _items;
        private int _builtItems;

        public override string Name {
            get {
                string title = _loot != null ? UiText.FirstLabel(_loot.gameObject) : null;
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _loot = top == null ? null : top.GetComponentInChildren<LootUiControllerBhv>(includeInactive: false);
            return _loot;
        }

        public override Container BuildRoot(object target) {
            var loot = (LootUiControllerBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: loot.ButtonClose);

            var description = FindChild(loot.transform, "Description");
            if (description != null) {
                // The live-guard matters: the closure can be read the frame the closing screen's
                // objects are destroyed, when the captured reference is Unity-dead but not null.
                _root.Add(new StaticTextElement(
                    () => description == null ? null : UiText.AllText(description.gameObject)));
            }

            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            PopulateItems(loot);

            var buttons = new Container(ContainerShape.VerticalList);
            AddButtonUnder(buttons, loot.transform, "TakeAllButton");
            AddButtonUnder(buttons, loot.transform, "CloseButton");
            AddButtonUnder(buttons, loot.transform, "CharSheetButton");
            AddButtonUnder(buttons, loot.transform, "InventoryBtn");
            _root.Add(buttons);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var loot = (LootUiControllerBhv)target;
            if (FilledSlots(loot) != _builtItems) {
                PopulateItems(loot);
            }
            return false;
        }

        private void PopulateItems(LootUiControllerBhv loot) {
            _items.Clear();
            var container = ItemContainerField(loot);
            if (container == null) {
                _builtItems = 0;
                return;
            }
            for (int i = 0; i < container.GetElementCount(); i++) {
                var item = container.GetElement(i);
                var selectable = item == null ? null : item.GetComponent<Selectable>();
                if (selectable != null) {
                    _items.Add(new InventoryItemElement(item, selectable));
                }
            }
            _builtItems = FilledSlots(loot);
        }

        private static int FilledSlots(LootUiControllerBhv loot) {
            var container = ItemContainerField(loot);
            return container?.Inventory == null ? 0 : container.Inventory.GetNumberOfFilledSlots();
        }

        // Prefab objects with no serialized field on the controller, located by their stable
        // names; logged loudly if the game renames them.
        private static Transform FindChild(Transform root, string name) {
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: false)) {
                if (child.name == name) {
                    return child;
                }
            }
            Plugin.Log.LogWarning("LootScreen: no '" + name + "' under the loot screen");
            return null;
        }

        private static void AddButtonUnder(Container container, Transform root, string name) {
            var holder = FindChild(root, name);
            if (holder == null) {
                return;
            }
            var button = holder.GetComponentInChildren<Button>(includeInactive: false);
            if (button != null) {
                container.Add(new SelectableElement(button, null, holder.gameObject));
            }
        }
    }
}
