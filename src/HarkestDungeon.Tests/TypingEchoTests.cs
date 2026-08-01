using System.Collections.Generic;
using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    public class TypingEchoTests {
        private bool _typing;
        private string _text = "";
        private readonly List<string> _spoken = new List<string>();
        private readonly TypingEcho _echo;

        public TypingEchoTests() {
            _echo = new TypingEcho(() => _typing, () => _text, (line, _) => _spoken.Add(line));
        }

        [Fact]
        public void EditStart_SpeaksOnce_AndSwallowsThePrefilledText() {
            _typing = true;
            _text = "Darkest";
            Assert.False(_echo.Tick());
            Assert.Equal(new[] { "editing, enter when done" }, _spoken);
            Assert.False(_echo.Tick());
            Assert.Equal(new[] { "editing, enter when done" }, _spoken);
        }

        [Fact]
        public void Keystrokes_EchoAdditionsAndDeletions() {
            _typing = true;
            _echo.Tick();
            _spoken.Clear();

            _text = "a";
            _echo.Tick();
            _text = "ab";
            _echo.Tick();
            _text = "ab ";
            _echo.Tick();
            _text = "ab";
            _echo.Tick();
            Assert.Equal(new[] { "a", "b", "space", "space deleted" }, _spoken);
        }

        [Fact]
        public void WholesaleChange_ReadsTheNewText_ButClearingIsSilent() {
            _typing = true;
            _text = "abc";
            _echo.Tick();
            _spoken.Clear();

            _text = "xyz";
            _echo.Tick();
            Assert.Equal(new[] { "xyz" }, _spoken);

            _spoken.Clear();
            _text = "";
            _echo.Tick();
            Assert.Empty(_spoken);
        }

        [Fact]
        public void EditEnd_RequestsTheAcceptedReadBack_Once() {
            _typing = true;
            _echo.Tick();
            _typing = false;
            Assert.True(_echo.Tick());
            Assert.False(_echo.Tick());
        }
    }
}
