using System;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Story;
using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// A road story (assistance, resistance, shrine, hero story): the story's title, then one
    /// element per choice - always a hero, speaking the choice itself (bark and previews), with
    /// per-line review in the buffer - then the utility buttons. S glances the focused choice's
    /// hero vitals. The narration itself is the game's own voiced narrator, already audible;
    /// this screen carries what the voice does not: who can be chosen and what each choice does.
    /// Escape is deliberately inert - a story blocks its screen from closing until resolved.
    /// </summary>
    public sealed class StoryScreen : GameScreen {
        private readonly Action<string, bool> _speak;
        private readonly TraditionalNavigator _navigator;
        private StoryScreenBhv _story;
        private Container _root;
        private Container _choices;
        private int _builtChoices;
        private bool _awaitingValue;
        private string _lastValue;

        public StoryScreen(Action<string, bool> speak, TraditionalNavigator navigator) {
            _speak = speak;
            _navigator = navigator;
        }

        private static readonly InputCategory[] StoryCategories =
            { InputCategory.Story, InputCategory.UI };
        public override InputCategory[] InputCategories => StoryCategories;

        public override string Name {
            get {
                string title = _story == null ? null : UiText.FirstLabel(FindChild(_story.transform, "TitleText")?.gameObject);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _story = top == null ? null : top.GetComponentInChildren<StoryScreenBhv>(includeInactive: false);
            return _story;
        }

        public override Container BuildRoot(object target) {
            var story = (StoryScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList);
            Populate(story);
            return _root;
        }

        /// <summary>The choice buttons exist before the story reaches its choose state, and the
        /// bark text binds a beat later still - an entry announced early reads a bare hero name.
        /// Every choosable choice carries a bark (the game localizes one per choice; a button
        /// whose choice is null stays disabled), so the entry holds until the landing choice's
        /// bark is readable.</summary>
        public override bool EntrySettled =>
            _choices.FirstFocusable() is StoryChoiceElement choice && choice.HasBark;

        public override bool OnUpdate(object target) {
            var story = (StoryScreenBhv)target;
            if (CountChoices(story) != _builtChoices) {
                _root.Clear();
                Populate(story);
            }
            return ValueSettled();
        }

        // A fresh story binds its bark text and choice previews a beat after the buttons
        // appear, so the entry read can land on a bare hero name. Watch the landing choice
        // until its value is non-empty and holds for a frame, then request the one
        // re-announce (deduped by the router when the entry already read the full line).
        private bool ValueSettled() {
            if (!_awaitingValue) {
                return false;
            }
            if (!(_choices.FirstFocusable() is StoryChoiceElement choice)) {
                _awaitingValue = false;
                return false;
            }
            string value = choice.Value;
            if (string.IsNullOrEmpty(value) || value != _lastValue) {
                _lastValue = value;
                return false;
            }
            _awaitingValue = false;
            _lastValue = null;
            return true;
        }

        /// <summary>S: the focused choice's hero vitals (name, HP, stress), spoken in place.
        /// Off a choice the key is silent.</summary>
        public void GlanceSelf() {
            string line = _navigator.Current is StoryChoiceElement choice ? choice.GlanceLine() : null;
            if (line != null) {
                _speak(line, true);
            }
        }

        private void Populate(StoryScreenBhv story) {
            var choices = new Container(ContainerShape.VerticalList);
            foreach (var button in story.GetComponentsInChildren<StoryChoiceButtonBhv>(includeInactive: false)) {
                choices.Add(new StoryChoiceElement(button));
            }
            _builtChoices = CountChoices(story);
            _root.Add(choices);
            _choices = choices;
            _awaitingValue = choices.FirstFocusable() is StoryChoiceElement;
            _lastValue = null;

            var buttons = new Container(ContainerShape.VerticalList);
            AddButtonUnder(buttons, story.transform, "CharSheetBtn");
            AddButtonUnder(buttons, story.transform, "MapBtn");
            AddButtonUnder(buttons, story.transform, "InventoryBtn");
            _root.Add(buttons);
        }

        private static int CountChoices(StoryScreenBhv story)
            => story.GetComponentsInChildren<StoryChoiceButtonBhv>(includeInactive: false).Length;

        private static Transform FindChild(Transform root, string name) {
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: false)) {
                if (child.name == name) {
                    return child;
                }
            }
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
