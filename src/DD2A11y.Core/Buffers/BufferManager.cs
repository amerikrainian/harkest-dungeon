using System;
using System.Collections.Generic;

namespace DD2A11y.Core.Buffers {
    /// <summary>
    /// The ordered buffer registry plus the review cursor across buffers. Cycling skips empty
    /// buffers; the current buffer may become empty under the cursor (a focus change rebound the
    /// sources), in which case navigation re-resolves to the first non-empty one.
    /// </summary>
    public sealed class BufferManager {
        private readonly List<Buffer> _buffers = new List<Buffer>();
        private int _position;

        public IReadOnlyList<Buffer> Buffers => _buffers;

        public Buffer Add(Buffer buffer) {
            _buffers.Add(buffer);
            return buffer;
        }

        public Buffer? Get(string key) {
            for (int i = 0; i < _buffers.Count; i++) {
                if (_buffers[i].Key == key) {
                    return _buffers[i];
                }
            }
            return null;
        }

        /// <summary>The buffer under the review cursor, re-resolved to the first non-empty buffer
        /// when the current one is empty. Null when every buffer is empty.</summary>
        public Buffer? Current {
            get {
                if (_buffers.Count == 0) {
                    return null;
                }
                if (!_buffers[_position].IsEmpty) {
                    return _buffers[_position];
                }
                for (int i = 0; i < _buffers.Count; i++) {
                    if (!_buffers[i].IsEmpty) {
                        _position = i;
                        return _buffers[i];
                    }
                }
                return null;
            }
        }

        /// <summary>Make a buffer current (a focus change re-homing review to the element's own
        /// buffer). Applies <see cref="Buffer.FollowLatest"/>.</summary>
        public void SetCurrent(string key) {
            for (int i = 0; i < _buffers.Count; i++) {
                if (_buffers[i].Key == key) {
                    _position = i;
                    if (_buffers[i].FollowLatest) {
                        _buffers[i].MoveToEnd();
                    }
                    return;
                }
            }
        }

        /// <summary>Step to the next/previous non-empty buffer, wrapping. False when no other
        /// non-empty buffer exists (including none at all).</summary>
        public bool MoveBuffer(int step) {
            if (_buffers.Count == 0) {
                return false;
            }
            for (int i = 1; i <= _buffers.Count; i++) {
                int idx = ((_position + step * i) % _buffers.Count + _buffers.Count) % _buffers.Count;
                if (idx == _position) {
                    break;
                }
                if (!_buffers[idx].IsEmpty) {
                    _position = idx;
                    if (_buffers[idx].FollowLatest) {
                        _buffers[idx].MoveToEnd();
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
