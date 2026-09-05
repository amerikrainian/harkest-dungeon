using Assets.Code.CommonLogic.Pooling;
using Assets.Code.Skill;
using Assets.Code.UI;
using Assets.Code.UI.Controllers;
using Assets.Code.UI.Items;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The loot screen (a battle's Victory rewards, a road cache): its description line, the
    /// reward texts the widget shows beside the items (mastery points, flame, coach gains, a
    /// completed contract, an origin skin - each with its tooltip in the buffer) and the
    /// skills a hero-story fight unlocked (name on the line, the skill card in the buffer),
    /// then each reward item - the item's own title and stack size, its full tooltip in the
    /// buffer, Enter taking it through the game's own transfer - then Take All / Leave Items
    /// and the utility buttons. Escape runs the game's own close flow, including its
    /// leave-items confirmation dialog when rewards remain.
    /// </summary>
    public sealed class LootScreen : GameScreen {
        private static readonly AccessTools.FieldRef<LootUiControllerBhv, LootInventoryItemContainerBhv> ItemContainerField =
            AccessTools.FieldRefAccess<LootUiControllerBhv, LootInventoryItemContainerBhv>("m_itemContainerBhv");
        private static readonly string[] RewardTextFields = {
            "m_masteryPointTextObj", "m_torchGainTextObj", "m_coachStatGainTextObj",
            "m_contractCompleteObj", "m_originSkinUnlockObj",
        };
        private static readonly System.Reflection.FieldInfo SkillsPoolField =
            AccessTools.Field(typeof(LootScreenWidgetBhv), "m_unlockedSkillsPool");

        private LootUiControllerBhv _loot;
        private Container _root;
        private Container _skills;
        private Container _items;
        private int _builtItems;
        private int _builtSkills;

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

        // The widget writes its title, description and reward texts in its open step, after
        // the push the router matches; the entry waits for the screen's own Open state.
        public override bool EntrySettled =>
            _loot != null && _loot.GetComponentInParent<UiScreenBhv>().ScreenState == UiScreenState.Open;

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

            AddRewards(loot);
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
            if (SkillsSignature(loot) != _builtSkills) {
                PopulateSkills(loot);
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

        // The widget's reward texts sit beside the items, each shown only when it has
        // something to say; the skill unlocks are pooled rows under their own container.
        private void AddRewards(LootUiControllerBhv loot) {
            var screen = loot.GetComponentInParent<UiScreenBhv>();
            var widget = screen == null ? null : screen.GetWidget<LootScreenWidgetBhv>();
            if (widget == null) {
                return;
            }
            foreach (var field in RewardTextFields) {
                AddRewardText(_root, AccessTools.Field(typeof(LootScreenWidgetBhv), field)?.GetValue(widget) as GameObject);
            }
            _skills = new Container(ContainerShape.VerticalList);
            _root.Add(_skills);
            PopulateSkills(loot);
        }

        // The unlocked-skill rows spawn in the widget's open step, after the push the router
        // matches, so they fill on an instance-id signature like the items.
        private void PopulateSkills(LootUiControllerBhv loot) {
            _skills.Clear();
            foreach (var unlock in SkillRows(loot)) {
                var captured = unlock;
                _skills.Add(new ReadoutElement(
                    () => {
                        var skill = captured == null ? null : Actors.Skill(captured.SkillId);
                        return skill == null ? null : SkillDescription.GetNameText(skill);
                    },
                    detail: () => captured == null ? System.Linq.Enumerable.Empty<string>() : SkillCard.Lines(captured.SkillId, 0u)));
            }
            _builtSkills = SkillsSignature(loot);
        }

        private static LootSkillUnlockBhv[] SkillRows(LootUiControllerBhv loot) {
            var screen = loot.GetComponentInParent<UiScreenBhv>();
            var widget = screen == null ? null : screen.GetWidget<LootScreenWidgetBhv>();
            var pool = widget == null ? null : SkillsPoolField?.GetValue(widget) as GameObjectPoolBhv;
            return pool == null ? new LootSkillUnlockBhv[0] : pool.GetComponentsInChildren<LootSkillUnlockBhv>(includeInactive: false);
        }

        private static int SkillsSignature(LootUiControllerBhv loot) {
            int signature = 17;
            foreach (var row in SkillRows(loot)) {
                signature = signature * 31 + row.GetInstanceID();
            }
            return signature;
        }

        /// <summary>A reward caption object (the "+1 Mastery" line and its kin) as a readout
        /// with its tooltip in the buffer. Read live: the widget shows and fills these in its
        /// open step, after the push the router matches, so an inactive one live-skips and a
        /// label the game never resolved (its colored missing-key marker) reads as nothing.</summary>
        internal static void AddRewardText(Container root, GameObject text) {
            if (text == null) {
                return;
            }
            root.Add(new ReadoutElement(() => RewardCaption(text), detail: () => TooltipReader.Lines(text)));
        }

        private static string RewardCaption(GameObject text) {
            if (text == null || !text.activeInHierarchy) {
                return null;
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var tmp in text.GetComponentsInChildren<TMPro.TMP_Text>(includeInactive: false)) {
                string part = GameLoc.DropMissingKeyMarker(tmp.text);
                if (!string.IsNullOrWhiteSpace(part)) {
                    parts.Add(part);
                }
            }
            return parts.Count == 0 ? null : string.Join(". ", parts);
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
