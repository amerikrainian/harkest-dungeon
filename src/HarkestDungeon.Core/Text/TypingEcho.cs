using System;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Text {
    /// <summary>
    /// Speaks what a game-owned text edit changes while the game's edit flow holds the keyboard:
    /// the edit start, then each keystroke (an addition echoes the character, a deletion names it,
    /// a wholesale swap reads the new text). Ticked once per frame by the owning screen; the tick
    /// returns true on the frame the edit ends so the screen can speak the accepted value its own
    /// way. Engine state comes in through the two delegates.
    /// </summary>
    public sealed class TypingEcho {
        private readonly Func<bool> _isTyping;
        private readonly Func<string> _fieldText;
        private readonly Action<string, bool> _speak;
        private bool _wasTyping;
        private string _typed = "";

        public TypingEcho(Func<bool> isTyping, Func<string> fieldText, Action<string, bool> speak) {
            _isTyping = isTyping;
            _fieldText = fieldText;
            _speak = speak;
        }

        /// <summary>Per-frame; true on the frame the edit just ended.</summary>
        public bool Tick() {
            bool typing = _isTyping();
            if (typing == _wasTyping) {
                if (typing && _fieldText() != _typed) {
                    EchoDiff(_typed, _fieldText());
                    _typed = _fieldText();
                }
                return false;
            }
            _wasTyping = typing;
            if (typing) {
                _typed = _fieldText();
                _speak(S.EditStarted, false);
                return false;
            }
            return true;
        }

        private void EchoDiff(string old, string now) {
            if (now.Length == old.Length + 1 && now.StartsWith(old, StringComparison.Ordinal)) {
                _speak(Echo(now[now.Length - 1]), true);
            } else if (old.Length == now.Length + 1 && old.StartsWith(now, StringComparison.Ordinal)) {
                _speak(S.EditDeleted(Echo(old[old.Length - 1])), true);
            } else if (now.Length > 0) {
                _speak(now, true); // a wholesale change (the edit start clearing the field is silent)
            }
        }

        private static string Echo(char c) => c == ' ' ? S.EditSpace : c.ToString();
    }
}
