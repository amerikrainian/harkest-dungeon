using System.Collections.Generic;
using System.Linq;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Nav;
using Xunit;

namespace DD2A11y.Tests {
    public class BufferTests {
        private static Buffer Make(string key = "ui") => new Buffer(key, () => key);

        [Fact]
        public void ReadsLinesFromSource_AndStepsWithinBounds() {
            var buffer = Make();
            buffer.SetSource(() => new[] { "one", "two" });
            Assert.Equal("one", buffer.CurrentLine);
            Assert.True(buffer.MoveNext());
            Assert.Equal("two", buffer.CurrentLine);
            Assert.False(buffer.MoveNext()); // edge
            Assert.True(buffer.MovePrevious());
            Assert.False(buffer.MovePrevious()); // edge
        }

        [Fact]
        public void RefreshRereadsLiveSource_PreservingPosition() {
            var lines = new List<string> { "a", "b", "c" };
            var buffer = Make();
            buffer.SetSource(() => lines);
            buffer.MoveNext();
            lines[1] = "b2"; // the live model changed under us
            Assert.Equal("b2", buffer.CurrentLine);
        }

        [Fact]
        public void PositionResetsWhenContentShrinksUnderTheCursor() {
            var lines = new List<string> { "a", "b", "c" };
            var buffer = Make();
            buffer.SetSource(() => lines);
            buffer.MoveNext();
            buffer.MoveNext();
            lines.RemoveRange(1, 2);
            Assert.Equal("a", buffer.CurrentLine);
        }

        [Fact]
        public void NullSourceEmpties() {
            var buffer = Make();
            buffer.SetSource(() => new[] { "x" });
            buffer.SetSource(null);
            Assert.True(buffer.IsEmpty);
            Assert.Null(buffer.CurrentLine);
        }

        [Fact]
        public void ManagerSkipsEmptyBuffers_AndWraps() {
            var manager = new BufferManager();
            var a = manager.Add(new Buffer("a", () => "a"));
            manager.Add(new Buffer("b", () => "b"));
            var c = manager.Add(new Buffer("c", () => "c"));
            a.SetSource(() => new[] { "line a" });
            c.SetSource(() => new[] { "line c" });

            manager.SetCurrent("a");
            Assert.True(manager.MoveBuffer(1)); // skips empty b
            Assert.Equal("c", manager.Current!.Key);
            Assert.True(manager.MoveBuffer(1)); // wraps
            Assert.Equal("a", manager.Current!.Key);
        }

        [Fact]
        public void ManagerCurrentIsNullWhenEverythingIsEmpty() {
            var manager = new BufferManager();
            manager.Add(new Buffer("a", () => "a"));
            Assert.Null(manager.Current);
            Assert.False(manager.MoveBuffer(1));
        }

        [Fact]
        public void FollowLatestJumpsToNewestLineOnSwitch() {
            var manager = new BufferManager();
            var events = manager.Add(new Buffer("events", () => "events") { FollowLatest = true });
            events.SetSource(() => new[] { "old", "new" });
            manager.SetCurrent("events");
            Assert.Equal("new", events.CurrentLine);
        }
    }

    public class BufferControlsTests {
        private readonly List<string> _spoken = new();
        private readonly BufferManager _manager = new();
        private readonly BufferControls _controls;

        public BufferControlsTests() {
            _controls = new BufferControls(_manager, (text, interrupt) => _spoken.Add(text));
        }

        [Fact]
        public void SwitchingBufferSpeaksNameAndCurrentLine() {
            _manager.Add(new Buffer("ui", () => "control")).SetSource(() => new[] { "Continue, button", "tooltip line" });
            _controls.NextBuffer();
            Assert.Equal("control: Continue, button", Assert.Single(_spoken));
        }

        [Fact]
        public void SteppingSpeaksJustTheLine_AndEdgesRepeat() {
            _manager.Add(new Buffer("ui", () => "control")).SetSource(() => new[] { "first", "second" });
            _controls.NextLine();
            Assert.Equal("second", _spoken[^1]);
            _controls.NextLine(); // at the edge: re-reads
            Assert.Equal("second", _spoken[^1]);
            _controls.PreviousLine();
            Assert.Equal("first", _spoken[^1]);
        }

        [Fact]
        public void NoContentSpeaksNoBuffers() {
            _manager.Add(new Buffer("ui", () => "control"));
            _controls.NextLine();
            Assert.Equal("no buffer lines", Assert.Single(_spoken));
        }
    }

    public class ElementBufferLinesTests {
        private sealed class FakeElement : UIElement {
            public string? L, R, V, S;
            public List<string> Details = new();
            public override string? Label => L;
            public override string? Role => R;
            public override string? Value => V;
            public override string? Status => S;
            protected override IEnumerable<string> GetDetailLines() => Details;
        }

        [Fact]
        public void HeadLine_CarriesNoRole() {
            var element = new FakeElement { L = "Confessions", R = "button" };
            Assert.Equal(new[] { "Confessions" }, element.GetBufferLines());
        }

        [Fact]
        public void HeadLine_KeepsStatusAndValue() {
            var element = new FakeElement { S = "on", L = "Tutorials", R = "toggle" };
            Assert.Equal("on, Tutorials", element.GetBufferLines().First());
        }

        [Fact]
        public void DetailRepeatingTheLabel_IsFolded() {
            var element = new FakeElement {
                L = "Healing Potion", R = "button",
                Details = { "Healing Potion", "Restores 20 health" },
            };
            Assert.Equal(new[] { "Healing Potion", "Restores 20 health" }, element.GetBufferLines());
        }

        [Fact]
        public void DetailRepeatingTheLabelThroughMarkup_IsFolded() {
            var element = new FakeElement {
                L = "Healing Potion",
                Details = { "<color=#aa0000>Healing Potion</color>", "Restores 20 health" },
            };
            Assert.Equal(new[] { "Healing Potion", "Restores 20 health" }, element.GetBufferLines());
        }

        [Fact]
        public void DetailRepeatingTheValue_IsFolded() {
            var element = new FakeElement {
                L = "Profile", V = "Traveler",
                Details = { "Traveler", "Switch profiles" },
            };
            Assert.Equal(new[] { "Profile, Traveler", "Switch profiles" }, element.GetBufferLines());
        }

        [Fact]
        public void BlankDetails_AreDropped() {
            var element = new FakeElement { L = "Row", Details = { "   ", "<b></b>", "kept" } };
            Assert.Equal(new[] { "Row", "kept" }, element.GetBufferLines());
        }
    }
}
