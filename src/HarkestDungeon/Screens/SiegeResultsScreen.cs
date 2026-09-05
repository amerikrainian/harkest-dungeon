using Assets.Code.Data;
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
    /// The Kingdoms siege results (a Loot-layer stack screen after a siege battle, victory or
    /// defeat), named by the game's own outcome label ("Siege Repelled!" / "Inn Destroyed!").
    /// Reads like the loot screen it is built on: the outcome's description, a defeat's hero
    /// effect lines, the reward texts (mastery points, flame, coach gains - each with its
    /// tooltip in the buffer), then each reward item, then Take All and Close. Escape is the
    /// widget's own close, including its leave-items confirmation. The floor read only a
    /// "Close" whose press was the panel's root button, which the widget refuses.
    /// </summary>
    public sealed class SiegeResultsScreen : GameScreen {
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, LootInventoryItemContainerBhv> ItemContainerField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, LootInventoryItemContainerBhv>("m_itemContainerBhv");
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, GameObject> MasteryTextField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, GameObject>("m_masteryPointTextObj");
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, GameObject> TorchTextField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, GameObject>("m_torchGainTextObj");
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, GameObject> CoachTextField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, GameObject>("m_coachStatGainTextObj");
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, Button> CloseButtonField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, Button>("m_onwardBtn");
        private static readonly AccessTools.FieldRef<SiegeResultsWidgetBhv, Button> TakeAllButtonField =
            AccessTools.FieldRefAccess<SiegeResultsWidgetBhv, Button>("m_takeAllBtn");

        private SiegeResultsWidgetBhv _widget;
        private Container _root;
        private Container _items;
        private int _builtItems;

        public override string Name {
            get {
                var context = _widget == null ? null : ContextField(_widget);
                string label = context == null ? null : GameLoc.TryGet(context.GetStringValue("siege_label"));
                return label ?? S.ScreenGeneric;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<SiegeResultsWidgetBhv>(includeInactive: false);
            return _widget;
        }

        // The widget's close and Take All refuse until the screen is Open.
        public override bool EntrySettled =>
            _widget != null && _widget.GetComponentInParent<UiScreenBhv>().ScreenState == UiScreenState.Open;

        public override Container BuildRoot(object target) {
            var widget = (SiegeResultsWidgetBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: widget.ButtonClose);
            var context = ContextField(widget);
            _root.Add(new StaticTextElement(() => context == null ? null : context.GetStringValue("siege_description")));
            // A defeat's hero effects, the way the widget lists them - read live, since it
            // writes them in its open step, after the push the router matches; a victory
            // writes none and the element live-skips.
            _root.Add(new ReadoutElement(
                () => EffectLines(context) == null ? null : Core.Text.SpokenLine.Join(", ", EffectLines(context)),
                detail: () => EffectLines(context) ?? System.Linq.Enumerable.Empty<string>()));
            LootScreen.AddRewardText(_root, MasteryTextField(widget));
            LootScreen.AddRewardText(_root, TorchTextField(widget));
            LootScreen.AddRewardText(_root, CoachTextField(widget));
            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            PopulateItems(widget);
            var buttons = new Container(ContainerShape.VerticalList);
            var takeAll = TakeAllButtonField(widget);
            if (takeAll != null) {
                buttons.Add(new SelectableElement(takeAll));
            }
            var close = CloseButtonField(widget);
            if (close != null) {
                buttons.Add(new SelectableElement(close));
            }
            _root.Add(buttons);
            return _root;
        }

        private static string[] EffectLines(DataContextBhv context) {
            string effects = context == null ? null : context.GetStringValue("siege_effect_description");
            if (string.IsNullOrWhiteSpace(effects)) {
                return null;
            }
            var lines = new System.Collections.Generic.List<string>();
            foreach (var line in effects.Split('\n')) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    lines.Add(line);
                }
            }
            return lines.Count == 0 ? null : lines.ToArray();
        }

        public override bool OnUpdate(object target) {
            var widget = (SiegeResultsWidgetBhv)target;
            if (FilledSlots(widget) != _builtItems) {
                PopulateItems(widget);
            }
            return false;
        }

        private void PopulateItems(SiegeResultsWidgetBhv widget) {
            _items.Clear();
            var container = ItemContainerField(widget);
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
            _builtItems = FilledSlots(widget);
        }

        private static int FilledSlots(SiegeResultsWidgetBhv widget) {
            var container = ItemContainerField(widget);
            return container?.Inventory == null ? 0 : container.Inventory.GetNumberOfFilledSlots();
        }
    }
}
