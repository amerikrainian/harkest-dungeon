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
        private int _builtChoices;

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

        public override bool OnUpdate(object target) {
            var story = (StoryScreenBhv)target;
            if (CountChoices(story) != _builtChoices) {
                _root.Clear();
                Populate(story);
            }
            return false;
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
