using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DD2A11y.Dev {
    /// <summary>A thread-safe append-only line ring with a cursor protocol: readers pass the
    /// cursor from their last read and get everything since, plus the next cursor.</summary>
    public sealed class LineLog {
        private const int Max = 5000;

        private readonly object _lock = new object();
        private readonly List<string> _lines = new List<string>();
        private int _base;

        public int Cursor {
            get {
                lock (_lock) {
                    return _base + _lines.Count;
                }
            }
        }

        public void Add(string line) {
            lock (_lock) {
                _lines.Add(line);
                if (_lines.Count > Max) {
                    int drop = _lines.Count - Max;
                    _lines.RemoveRange(0, drop);
                    _base += drop;
                }
                Monitor.PulseAll(_lock);
            }
        }

        public string Read(int since, out int next) {
            lock (_lock) {
                next = _base + _lines.Count;
                var sb = new StringBuilder();
                for (int i = Math.Max(since, _base); i < next; i++) {
                    sb.Append(i).Append(": ").Append(_lines[i - _base]).Append('\n');
                }
                return sb.ToString();
            }
        }

        /// <summary>Block until a line lands past <paramref name="since"/> or the timeout passes.</summary>
        public bool WaitForMore(int since, int timeoutMs) {
            var deadline = Environment.TickCount + timeoutMs;
            lock (_lock) {
                while (_base + _lines.Count <= since) {
                    int remaining = deadline - Environment.TickCount;
                    if (remaining <= 0 || !Monitor.Wait(_lock, remaining)) {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
