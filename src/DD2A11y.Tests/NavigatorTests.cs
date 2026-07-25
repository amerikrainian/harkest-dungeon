using System;
using System.Collections.Generic;
using DD2A11y.Core.Nav;
using Xunit;

namespace DD2A11y.Tests {
    internal sealed class TestElement : UIElement {
        private readonly string _label;
        private readonly string? _role;
        public int Activations;
        public int Level; // a fake adjustable value; -1 = not adjustable
        public bool Adjustable;

        public TestElement(string label, string? role = null) {
            _label = label;
            _role = role;
        }

        public override string Label => _label;
        public override string? Role => _role;
        public override string? Value => Adjustable ? Level.ToString() : null;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => Activations++);
            if (Adjustable) {
                yield return new ElementAction(ActionIds.Increase, () => Level = Math.Min(Level + 1, 2));
                yield return new ElementAction(ActionIds.Decrease, () => Level = Math.Max(Level - 1, 0));
            }
        }
    }

    public class NavigatorTests {
        private readonly List<string> _spoken = new();
        private readonly TraditionalNavigator _nav;

        public NavigatorTests() {
            _nav = new TraditionalNavigator((text, interrupt) => _spoken.Add(text));
        }

        private static Container VerticalMenu(params UIElement[] items) {
            var root = new Container(ContainerShape.VerticalList);
            foreach (var item in items) {
                root.Add(item);
            }
            return root;
        }

        [Fact]
        public void AttachLandsOnFirstFocusable_AndAnnouncesOnRequest() {
            _nav.Attach(VerticalMenu(new TestElement("Continue", "button"), new TestElement("Quit", "button")));
            _nav.AnnounceCurrent();
            Assert.Equal("Continue, button", Assert.Single(_spoken));
        }

        [Fact]
        public void DownMovesAndAnnouncesOnlyTheNewFocus() {
            _nav.Attach(VerticalMenu(new TestElement("A"), new TestElement("B")));
            Assert.True(_nav.Handle(UiActions.Down));
            Assert.Equal("B", Assert.Single(_spoken));
        }

        [Fact]
        public void CaptionHotkeyActivatesTheAdvertisingButton_WithoutMovingFocus() {
            var map = new TestElement("Map (M)", "button");
            var nested = VerticalMenu(map);
            var first = new TestElement("Continue", "button");
            var root = VerticalMenu(first, nested);
            _nav.Attach(root);

            Assert.True(_nav.ActivateCaptionHotkey("(M)"));
            Assert.Equal(1, map.Activations);
            Assert.Same(first, _nav.Current);
            Assert.False(_nav.ActivateCaptionHotkey("(Z)"));
        }

        [Fact]
        public void EdgesConsumeWithoutWrapping() {
            _nav.Attach(VerticalMenu(new TestElement("A"), new TestElement("B")));
            Assert.False(_nav.Handle(UiActions.Up)); // top edge: no spill target
            Assert.Empty(_spoken);
        }

        [Fact]
        public void EnterActivates_AndUnactionableKeyIsUnconsumed() {
            var item = new TestElement("A");
            _nav.Attach(VerticalMenu(item));
            Assert.True(_nav.Handle(UiActions.Activate));
            Assert.Equal(1, item.Activations);
        }

        [Fact]
        public void LeftRightAdjustAFocusedStepper_AndAnnounceBounds() {
            var slider = new TestElement("Volume", "slider") { Adjustable = true, Level = 1 };
            _nav.Attach(VerticalMenu(slider));
            _nav.Handle(UiActions.Right);
            Assert.Equal("2", _spoken[^1]);
            _nav.Handle(UiActions.Right); // clamped at 2
            Assert.Equal("maximum", _spoken[^1]);
            _nav.Handle(UiActions.Left);
            _nav.Handle(UiActions.Left);
            _nav.Handle(UiActions.Left); // clamped at 0
            Assert.Equal("minimum", _spoken[^1]);
        }

        [Fact]
        public void VerticalSpillFlowsBetweenStackedBlocks() {
            var top = new Container(ContainerShape.HorizontalList, "party");
            top.Add(new TestElement("Hero1"));
            top.Add(new TestElement("Hero2"));
            var bottom = new Container(ContainerShape.VerticalList);
            bottom.Add(new TestElement("Embark"));
            var root = new Container(ContainerShape.VerticalList);
            root.Add(top);
            root.Add(bottom);

            _nav.Attach(root);
            Assert.Equal("Hero1", _nav.Current!.Label);
            Assert.True(_nav.Handle(UiActions.Down)); // spill out of the strip into the block below
            Assert.Equal("Embark", _nav.Current!.Label);
            Assert.True(_nav.Handle(UiActions.Up));   // and back up to the remembered hero
            Assert.Equal("Hero1", _nav.Current!.Label);
        }

        [Fact]
        public void ContainerEntryAnnouncesItsLabelBeforeTheLanding() {
            var strip = new Container(ContainerShape.HorizontalList, "roster");
            strip.Add(new TestElement("Hero"));
            var root = new Container(ContainerShape.VerticalList);
            root.Add(new TestElement("Embark"));
            root.Add(strip);

            _nav.Attach(root);
            _spoken.Clear();
            _nav.Handle(UiActions.Down);
            Assert.Equal("roster, Hero", Assert.Single(_spoken));
        }

        [Fact]
        public void EnsureFocusValid_RehomesAfterRebuild() {
            var root = VerticalMenu(new TestElement("A"), new TestElement("B"));
            _nav.Attach(root);
            _nav.Handle(UiActions.Down);
            root.Clear();
            root.Add(new TestElement("New"));
            Assert.True(_nav.EnsureFocusValid());
            Assert.Equal("New", _nav.Current!.Label);
            Assert.False(_nav.EnsureFocusValid()); // stable now
        }

        [Fact]
        public void FocusSettledFiresOnEveryLanding() {
            UIElement? settled = null;
            _nav.FocusSettled += element => settled = element;
            _nav.Attach(VerticalMenu(new TestElement("A"), new TestElement("B")));
            _nav.AnnounceCurrent();
            Assert.Equal("A", settled!.Label);
            _nav.Handle(UiActions.Down);
            Assert.Equal("B", settled!.Label);
        }

        [Fact]
        public void HomeEndJumpToListEdges() {
            _nav.Attach(VerticalMenu(new TestElement("A"), new TestElement("B"), new TestElement("C")));
            _nav.Handle(UiActions.End);
            Assert.Equal("C", _nav.Current!.Label);
            _nav.Handle(UiActions.Home);
            Assert.Equal("A", _nav.Current!.Label);
        }
    }
}
