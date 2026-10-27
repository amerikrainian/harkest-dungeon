using System;
using System.Collections.Generic;
using System.Linq;
using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using Xunit;

namespace DD2A11y.Tests {
    /// <summary>
    /// The context-sensitive key help: a key is listed only where it would do something - asked
    /// of the dispatcher without acting, else of the handler's own availability - plus the
    /// navigator's dry run it leans on, the quiet popup close and the speech hold the help runs
    /// a key under.
    /// </summary>
    public class KeyHelpTests {
        private sealed class FakeKey : InputBinding {
            private readonly string _name;
            public bool Down;

            public FakeKey(string name) => _name = name;

            public override string DisplayName => _name;
            public override bool JustPressed() => Down;
            public override bool Held() => Down;
            public override bool Released() => false;
            public override string Type => "fake";
            public override string Serialize() => _name;
        }

        private sealed class PadKey : InputBinding {
            public override string DisplayName => "A button";
            public override bool JustPressed() => false;
            public override bool Held() => false;
            public override bool Released() => false;
            public override string Type => "pad";
            public override string Serialize() => "a";
        }

        private sealed class RecordingBackend : ISpeechBackend {
            public readonly List<string> Spoken = new List<string>();
            public bool IsAvailable => true;
            public void Speak(string text, bool interrupt) => Spoken.Add((interrupt ? "!" : "") + text);
            public void Stop() { }
        }

        private static InputAction Action(InputManager input, string key, InputCategory category,
                                          Action? handler = null, Func<bool>? when = null) {
            var action = input.Register(key, () => key, category, handler).AddBinding(new FakeKey(key));
            return when == null ? action : action.When(when);
        }

        private static string[] Keys(IEnumerable<InputAction> actions) => actions.Select(a => a.Key).ToArray();

        [Fact]
        public void Collect_ListsOnlyKeysThatWouldAct_MostParticularFirst() {
            var input = new InputManager();
            Action(input, UiActions.Up, InputCategory.UI);
            Action(input, UiActions.Down, InputCategory.UI);
            Action(input, UiActions.Activate, InputCategory.UI);
            Action(input, UiActions.Back, InputCategory.UI);
            Action(input, "ui.inspect", InputCategory.UI, () => { }, when: () => false);
            Action(input, "ui.discard", InputCategory.UI, () => { }, when: () => true);
            Action(input, "ui.broken", InputCategory.UI, () => { }, when: () => throw new InvalidOperationException("gone"));
            Action(input, "buffer.next", InputCategory.UI, () => { });
            Action(input, "combat.enemy.1", InputCategory.Combat, () => { }, when: () => true);
            Action(input, "combat.enemy.2", InputCategory.Combat, () => { }, when: () => false);
            Action(input, "story.hero", InputCategory.Story, () => { });
            Action(input, "mod.standdown", InputCategory.Global, () => { });
            Action(input, "mod.help", InputCategory.Global, () => { });

            // Focus on the first of two rows: Down has somewhere to go, Up does not; Enter
            // answers; the screen takes Escape to no effect. The story key's category is dead.
            var dryRun = new Dictionary<string, bool?> {
                [UiActions.Down] = true,
                [UiActions.Activate] = true,
                [UiActions.Back] = false,
            };
            var warnings = new List<string>();
            var listed = KeyHelp.Collect(input.Actions, a => a.Key != "story.hero",
                a => a.Key.StartsWith("ui.", StringComparison.Ordinal) && dryRun.TryGetValue(a.Key, out var r) ? r : null,
                warnings.Add, a => a.Key == "mod.help");

            // The screen's own category, the UI keys that say when they apply, what the
            // dispatcher answers, the always-on UI keys, the global ones.
            Assert.Equal(new[] { "combat.enemy.1", "ui.discard", UiActions.Down, UiActions.Activate, "buffer.next", "mod.standdown" },
                Keys(listed));
            // A failing check is reported and leaves its key out.
            Assert.Contains("ui.broken", Assert.Single(warnings));
        }

        [Fact]
        public void KeysOf_LeavesOutFilteredBindings_ButNeverAll() {
            var input = new InputManager();
            var action = Action(input, UiActions.Activate, InputCategory.UI).AddBinding(new FakeKey("Numpad Enter"))
                .AddBinding(new PadKey());
            Assert.Equal("ui.activate, Numpad Enter, A button", KeyHelp.KeysOf(action));
            Assert.Equal("ui.activate, Numpad Enter", KeyHelp.KeysOf(action, b => !(b is PadKey)));
            Assert.Equal("ui.activate, Numpad Enter, A button", KeyHelp.KeysOf(action, _ => false));
        }

        [Fact]
        public void WouldHandle_FollowsTheFocus_WithoutMovingIt() {
            var spoken = new List<string>();
            var nav = new TraditionalNavigator((text, interrupt) => spoken.Add(text));
            var first = new TestElement("Continue");
            var second = new TestElement("Quit");
            var root = new Container(ContainerShape.VerticalList);
            root.Add(first);
            root.Add(second);
            nav.Attach(root);

            Assert.True(nav.WouldHandle(UiActions.Down));
            Assert.False(nav.WouldHandle(UiActions.Up));
            Assert.False(nav.WouldHandle(UiActions.Left)); // not adjustable, nowhere sideways
            Assert.True(nav.WouldHandle(UiActions.Activate));
            Assert.False(nav.WouldHandle(UiActions.Home)); // already on the first
            Assert.True(nav.WouldHandle(UiActions.End));
            Assert.False(nav.WouldHandle(UiActions.Next)); // one tab stop
            Assert.False(nav.WouldHandle(UiActions.Back)); // the root has no back action
            Assert.Same(first, nav.Current);
            Assert.Empty(spoken);

            Assert.True(nav.Handle(UiActions.Down));
            Assert.True(nav.WouldHandle(UiActions.Up));
            Assert.False(nav.WouldHandle(UiActions.Down));
            Assert.True(nav.WouldHandle(UiActions.Home));
            Assert.False(nav.WouldHandle(UiActions.End));
        }

        [Fact]
        public void WouldHandle_SeesAdjustsSpillsTabStopsAndPopups() {
            var nav = new TraditionalNavigator((text, interrupt) => { });
            var slider = new TestElement("Volume") { Adjustable = true };
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            var row = new Container(ContainerShape.HorizontalList);
            row.Add(slider);
            var list = new Container(ContainerShape.VerticalList);
            list.Add(row);
            list.Add(dropdown);
            var other = new Container(ContainerShape.VerticalList);
            other.Add(new TestElement("Done"));
            var root = new Container(ContainerShape.Panel);
            root.Add(list);
            root.Add(other);
            nav.Attach(root);

            Assert.Same(slider, nav.Current);
            Assert.True(nav.WouldHandle(UiActions.Right)); // adjusts in place
            Assert.True(nav.WouldHandle(UiActions.Down));  // spills out of the row
            Assert.True(nav.WouldHandle(UiActions.Next));  // another tab stop
            Assert.False(nav.WouldHandle(UiActions.Prev)); // first stop, no wrap

            nav.Handle(UiActions.Down);
            Assert.Same(dropdown, nav.Current);
            Assert.True(nav.WouldHandle(UiActions.Activate)); // opens its popup
            nav.Handle(UiActions.Activate);
            Assert.True(nav.PopupOpen);
            Assert.True(nav.WouldHandle(UiActions.Back)); // closes the popup
            Assert.Equal(0, dropdown.Choice);
        }

        [Fact]
        public void QuietPopupClose_RestoresFocusWithoutAWord() {
            var spoken = new List<string>();
            var nav = new TraditionalNavigator((text, interrupt) => spoken.Add(text));
            var dropdown = new TestDropdown("Windowed", "Fullscreen");
            var root = new Container(ContainerShape.VerticalList);
            root.Add(new TestElement("Back"));
            root.Add(dropdown);
            nav.Attach(root);
            nav.Handle(UiActions.Down);
            nav.Handle(UiActions.Activate);
            spoken.Clear();

            nav.ClosePopup(announce: false);
            Assert.False(nav.PopupOpen);
            Assert.Same(dropdown, nav.Current);
            Assert.Equal(1, dropdown.PopupCloses);
            Assert.Empty(spoken);
        }

        [Fact]
        public void Hold_FirstLineInterrupts_TheRestQueueAndAreRemembered() {
            var backend = new RecordingBackend();
            double now = 0;
            var speech = new SpeechPipeline(backend) { Clock = () => now };

            // The first line of a hold interrupts whatever it asked for: it cuts off the help
            // row that was being read when Enter was pressed. What follows queues, in order.
            speech.Speak("Discard item: Shift+Enter", interrupt: true);
            speech.BeginHold();
            speech.Speak("Grabbed Torch", interrupt: false);
            speech.Speak("Torch, 3", interrupt: true);
            Assert.Equal(new[] { "!Discard item: Shift+Enter", "!Grabbed Torch", "Torch, 3" }, backend.Spoken);
            Assert.Equal(new[] { "Grabbed Torch", "Torch, 3" }, speech.Held);

            speech.EndHold();
            speech.Speak("moved", interrupt: true);
            Assert.Equal("!moved", backend.Spoken.Last());

            // A hold nobody ends lapses by itself.
            speech.BeginHold();
            now += 10;
            Assert.False(speech.Holding);
            speech.Speak("later", interrupt: true);
            Assert.Equal("!later", backend.Spoken.Last());
        }

        [Fact]
        public void Admits_KillsRefusedKeys_AndPressedSeesOnlyRealPresses() {
            var input = new InputManager { ActiveCategoriesProvider = () => new[] { InputCategory.UI } };
            int kept = 0, dropped = 0, presses = 0;
            var keep = input.Register("ui.keep", () => "keep", InputCategory.UI, () => kept++);
            var keepKey = new FakeKey("K");
            keep.AddBinding(keepKey);
            var drop = input.Register("ui.drop", () => "drop", InputCategory.UI, () => dropped++);
            var dropKey = new FakeKey("D");
            drop.AddBinding(dropKey);
            input.Admits = a => a == keep;
            input.Pressed = () => presses++;

            keepKey.Down = true;
            dropKey.Down = true;
            input.Tick(0);
            Assert.True(input.IsLive(keep));
            Assert.False(input.IsLive(drop));
            Assert.Equal(1, kept);
            Assert.Equal(0, dropped);
            Assert.Equal(1, presses);

            // The help's own dispatch is not a press of the player's.
            Assert.True(input.FireAction("ui.drop"));
            Assert.Equal(1, dropped);
            Assert.Equal(1, presses);
        }
    }
}
