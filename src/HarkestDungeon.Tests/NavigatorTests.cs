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

    /// <summary>A dropdown-style element: activation opens a popup of its options, committing an
    /// option updates <see cref="Choice"/>.</summary>
    internal sealed class TestDropdown : UIElement {
        public readonly string[] Options;
        public int Choice;
        public int PopupCloses;

        public TestDropdown(params string[] options) => Options = options;

        public override string Label => "Window Mode";
        public override string? Role => "dropdown";
        public override string? Value => Options[Choice];

        public override Popup BuildPopup() {
            var list = new Container(ContainerShape.VerticalList, Label);
            for (int i = 0; i < Options.Length; i++) {
                int index = i;
                list.Add(new PopupOption(Options[index], () => Choice = index));
            }
            return new Popup(list, () => PopupCloses++);
        }

        private sealed class PopupOption : UIElement {
            private readonly string _label;
            private readonly Action _commit;
            public PopupOption(string label, Action commit) { _label = label; _commit = commit; }
            public override string Label => _label;
            public override IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, _commit);
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
        public void DetachSettlesOnNothing_SoBufferListenersUnbind() {
            UIElement? settled = null;
            bool fired = false;
            _nav.Attach(VerticalMenu(new TestElement("A")));
            _nav.FocusSettled += element => { settled = element; fired = true; };
            _nav.Attach(null);
            Assert.True(fired);
            Assert.Null(settled);
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
        public void RebuildReLandOnAnIdenticalLineStaysSilent() {
            var root = VerticalMenu(new TestElement("Set Route", "button"));
            _nav.Attach(root);
            _nav.AnnounceCurrent();
            _spoken.Clear();
            // The game populates a beat after entry: a rebuild replaces the element with a
            // brand-new one carrying the same text.
            root.Clear();
            root.Add(new TestElement("Set Route", "button"));
            Assert.True(_nav.EnsureFocusValid());
            _nav.AnnounceCurrentIfChanged();
            Assert.Empty(_spoken);
        }

        [Fact]
        public void RebuildReLandOnAChangedLineAnnounces() {
            var root = VerticalMenu(new TestElement("Enemies:"));
            _nav.Attach(root);
            _nav.AnnounceCurrent();
            _spoken.Clear();
            root.Clear();
            root.Add(new TestElement("Enemies: Cadavers"));
            Assert.True(_nav.EnsureFocusValid());
            _nav.AnnounceCurrentIfChanged();
            Assert.Equal("Enemies: Cadavers", Assert.Single(_spoken));
        }

        [Fact]
        public void RebuildReLandAfterAnAdjustReAnnounceStaysSilent() {
            // A stepper whose adjust rebuilds the screen under it (the trainer's hero
            // pager): the adjust feedback is the announcement, and the re-land on the
            // element's replacement repeats it verbatim - one utterance total.
            var pager = new TestElement("Hero", "hero") { Adjustable = true, Level = 1 };
            var root = VerticalMenu(pager);
            _nav.Attach(root);
            _nav.AnnounceCurrent();
            _spoken.Clear();
            Assert.True(_nav.Handle(UiActions.Right));
            root.Clear();
            root.Add(new TestElement("Hero", "hero") { Adjustable = true, Level = 2 });
            Assert.True(_nav.EnsureFocusValid());
            _nav.AnnounceCurrentIfChanged();
            Assert.Equal("2", Assert.Single(_spoken));
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
        public void VerticalSpillClimbsPastAnInnerListAtItsEdge() {
            // The altar track panels: a readout above an inner list of horizontal hero rows.
            // Up from the first row must climb out of the inner list to reach the readout.
            var row = new Container(ContainerShape.HorizontalList, "hero");
            row.Add(new TestElement("Icon"));
            row.Add(new TestElement("Milestone"));
            var rows = new Container(ContainerShape.VerticalList);
            rows.Add(row);
            var root = new Container(ContainerShape.VerticalList);
            var balance = new TestElement("Candles");
            root.Add(balance);
            root.Add(rows);

            _nav.Attach(root);
            _nav.Handle(UiActions.Down); // into the row
            _nav.Handle(UiActions.Right); // off the row head, so the climb starts deep
            Assert.Equal("Milestone", _nav.Current!.Label);
            Assert.True(_nav.Handle(UiActions.Up));
            Assert.Same(balance, _nav.Current);
            Assert.True(_nav.Handle(UiActions.Down)); // and back into the remembered row spot
            Assert.Equal("Milestone", _nav.Current!.Label);
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
        public void EnterOnDropdownOpensPopup_LandingOnFirstOptionUnmarked() {
            var dropdown = new TestDropdown("Windowed", "Fullscreen") { Choice = 1 };
            _nav.Attach(VerticalMenu(dropdown));
            _spoken.Clear();

            Assert.True(_nav.Handle(UiActions.Activate));
            Assert.True(_nav.PopupOpen);
            // The popup announces like a screen entry: its label, then the landing - always the
            // first option, never the current choice nor any selected marker on it.
            Assert.Equal(new[] { "Window Mode", "Windowed" }, _spoken);
            Assert.Equal("Windowed", _nav.Current!.Label);
        }

        [Fact]
        public void EnterOnPopupOptionCommits_ClosingAndReadingBackTheNewValue() {
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            _nav.Attach(VerticalMenu(dropdown));
            _nav.Handle(UiActions.Activate);
            _nav.Handle(UiActions.Down);
            _spoken.Clear();

            Assert.True(_nav.Handle(UiActions.Activate));
            Assert.Equal(1, dropdown.Choice);
            Assert.False(_nav.PopupOpen);
            Assert.Equal(1, dropdown.PopupCloses);
            Assert.Same(dropdown, _nav.Current);
            // The restored dropdown line is the whole feedback; it carries the new value.
            Assert.Equal("Window Mode, dropdown, Fullscreen", Assert.Single(_spoken));
        }

        [Fact]
        public void EscapeCancelsPopup_RestoringAndReannouncingTheDropdown() {
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            _nav.Attach(VerticalMenu(dropdown));
            _nav.Handle(UiActions.Activate);
            _nav.Handle(UiActions.Down);
            _spoken.Clear();

            Assert.True(_nav.Handle(UiActions.Back));
            Assert.Equal(0, dropdown.Choice);
            Assert.False(_nav.PopupOpen);
            Assert.Equal(1, dropdown.PopupCloses);
            Assert.Same(dropdown, _nav.Current);
            Assert.Equal("Window Mode, dropdown, Windowed", Assert.Single(_spoken));
        }

        [Fact]
        public void PopupFocusRestoresByReference_NotToTheFirstElement() {
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            var root = VerticalMenu(new TestElement("Resolution"), dropdown);
            _nav.Attach(root);
            _nav.Handle(UiActions.Down); // onto the dropdown, second in the list
            _nav.Handle(UiActions.Activate);

            _nav.Handle(UiActions.Back);
            Assert.Same(dropdown, _nav.Current);
        }

        [Fact]
        public void AttachClosesAnOpenPopup_RunningItsCloseHook() {
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            _nav.Attach(VerticalMenu(dropdown));
            _nav.Handle(UiActions.Activate);
            Assert.True(_nav.PopupOpen);

            _nav.Attach(VerticalMenu(new TestElement("Elsewhere")));
            Assert.False(_nav.PopupOpen);
            Assert.Equal(1, dropdown.PopupCloses);
        }

        [Fact]
        public void PopupBoundsClampWithoutSpillingIntoTheScreen() {
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            var root = VerticalMenu(new TestElement("Resolution"), dropdown, new TestElement("VSync"));
            _nav.Attach(root);
            _nav.Handle(UiActions.Down);
            _nav.Handle(UiActions.Activate);
            _spoken.Clear();

            _nav.Handle(UiActions.Up); // top edge: must not spill onto the screen behind
            Assert.True(_nav.PopupOpen);
            Assert.Equal("Windowed", _nav.Current!.Label);
            _nav.Handle(UiActions.Down);
            _nav.Handle(UiActions.Down); // bottom edge: clamped
            Assert.Equal("Fullscreen", _nav.Current!.Label);
        }

        [Fact]
        public void HomeEndJumpToListEdges() {
            _nav.Attach(VerticalMenu(new TestElement("A"), new TestElement("B"), new TestElement("C")));
            _nav.Handle(UiActions.End);
            Assert.Equal("C", _nav.Current!.Label);
            _nav.Handle(UiActions.Home);
            Assert.Equal("A", _nav.Current!.Label);
        }

        [Fact]
        public void HomeEndSpanTheScreenAcrossNestedLists_WhenNoPanelSplitsIt() {
            // The story screen's shape: one vertical flow holding the choices list then the
            // utility buttons list - End reaches the last button, Home the first hero.
            var root = VerticalMenu(
                VerticalMenu(new TestElement("Hero A"), new TestElement("Hero B")),
                VerticalMenu(new TestElement("Map"), new TestElement("Inventory")));
            _nav.Attach(root);
            _nav.Handle(UiActions.End);
            Assert.Equal("Inventory", _nav.Current!.Label);
            _nav.Handle(UiActions.Home);
            Assert.Equal("Hero A", _nav.Current!.Label);
        }

        [Fact]
        public void HomeEndStayInsideTheirPanel() {
            var left = VerticalMenu(new TestElement("A"), new TestElement("B"));
            var right = VerticalMenu(new TestElement("X"), new TestElement("Y"));
            var root = new Container(ContainerShape.Panel);
            root.Add(left);
            root.Add(right);
            _nav.Attach(root);

            _nav.Handle(UiActions.End);
            Assert.Equal("B", _nav.Current!.Label); // clamped to the left panel, not "Y"
            _nav.Handle(UiActions.Home);
            Assert.Equal("A", _nav.Current!.Label);

            _nav.Handle(UiActions.Next); // Tab crosses into the right panel
            Assert.Equal("X", _nav.Current!.Label);
            _nav.Handle(UiActions.End);
            Assert.Equal("Y", _nav.Current!.Label);
        }

        [Fact]
        public void HomeEndAreInertOnALoneLeafTabStop() {
            var root = new Container(ContainerShape.Panel);
            root.Add(new TestElement("Close", "button"));
            root.Add(VerticalMenu(new TestElement("A"), new TestElement("B")));
            _nav.Attach(root);

            Assert.Equal("Close", _nav.Current!.Label);
            Assert.True(_nav.Handle(UiActions.End)); // consumed, focus holds
            Assert.Equal("Close", _nav.Current!.Label);
        }
    }
}
