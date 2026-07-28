using System;
using static DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Buffers {
    /// <summary>
    /// The four buffer review commands, composed and spoken here so every screen shares one
    /// behavior: switching a buffer speaks its name and current line; stepping speaks just the
    /// line; the edges re-read the current line rather than going silent. Speech is injected so
    /// this stays unit-testable.
    /// </summary>
    public sealed class BufferControls {
        private readonly BufferManager _buffers;
        private readonly Action<string, bool> _speak;

        public BufferControls(BufferManager buffers, Action<string, bool> speak) {
            _buffers = buffers;
            _speak = speak;
        }

        public void NextBuffer() {
            _buffers.MoveBuffer(1);
            ReportBuffer();
        }

        public void PreviousBuffer() {
            _buffers.MoveBuffer(-1);
            ReportBuffer();
        }

        public void NextLine() {
            var buffer = _buffers.Current;
            if (buffer == null) {
                _speak(BufferNone, true);
                return;
            }
            buffer.MoveNext();
            ReportLine(buffer);
        }

        public void PreviousLine() {
            var buffer = _buffers.Current;
            if (buffer == null) {
                _speak(BufferNone, true);
                return;
            }
            buffer.MovePrevious();
            ReportLine(buffer);
        }

        private void ReportBuffer() {
            var buffer = _buffers.Current;
            if (buffer == null) {
                _speak(BufferNone, true);
                return;
            }
            _speak(BufferLine(buffer.Label, buffer.CurrentLine ?? ""), true);
        }

        private void ReportLine(Buffer buffer) {
            var line = buffer.CurrentLine;
            if (line == null) {
                _speak(BufferNone, true);
                return;
            }
            _speak(line, true);
        }
    }
}
