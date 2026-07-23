using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using Mono.CSharp;
using UnityEngine.EventSystems;

namespace DD2A11y.Dev {
    /// <summary>
    /// The loopback dev driver (Debug builds): a minimal HTTP server on 127.0.0.1 that lets an
    /// agent introspect and drive the live game - see CLAUDE.md for the endpoint contract. Worker
    /// threads parse requests and marshal game-touching work onto the main thread via the pump.
    /// </summary>
    public sealed class DevServer : IDisposable {
        private readonly Runtime _runtime;
        private readonly TcpListener _listener;
        private readonly ConcurrentQueue<Action> _mainQueue = new ConcurrentQueue<Action>();
        private readonly List<WaitJob> _waitJobs = new List<WaitJob>();
        private readonly EvalHost _eval = new EvalHost();
        private volatile bool _stopped;

        public readonly LineLog SpeechLog = new LineLog();
        public readonly LineLog GameLog = new LineLog();

        private sealed class WaitJob {
            public CompiledMethod Predicate;
            public ManualResetEventSlim Done;
            public volatile bool Result;
        }

        private sealed class LogTap : ILogListener {
            private readonly LineLog _log;
            public LogTap(LineLog log) => _log = log;
            public void LogEvent(object sender, LogEventArgs eventArgs)
                => _log.Add($"[{eventArgs.Level,-7}:{eventArgs.Source.SourceName}] {eventArgs.Data}");
            public void Dispose() { }
        }

        public static DevServer TryStart(Runtime runtime) {
            if (Environment.GetEnvironmentVariable("DD2A11Y_NO_DEV") == "1") {
                return null;
            }
            int port = 8771;
            string portVar = Environment.GetEnvironmentVariable("DD2A11Y_DEV_PORT");
            if (!string.IsNullOrEmpty(portVar) && int.TryParse(portVar, out int parsed)) {
                port = parsed;
            }
            try {
                var server = new DevServer(runtime, port);
                Plugin.Log.LogInfo($"dev http: listening on 127.0.0.1:{port}");
                return server;
            } catch (Exception ex) {
                Plugin.Log.LogError("dev http: failed to start: " + ex.Message);
                return null;
            }
        }

        private DevServer(Runtime runtime, int port) {
            _runtime = runtime;
            SpeechPipeline.Spoken = (text, interrupt, source)
                => SpeechLog.Add($"[{(interrupt ? "interrupt" : "queue")}] [{source}] {text}");
            BepInEx.Logging.Logger.Listeners.Add(new LogTap(GameLog));
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            var thread = new Thread(AcceptLoop) { IsBackground = true, Name = "DD2A11y dev http" };
            thread.Start();
        }

        public void Dispose() {
            _stopped = true;
            try {
                _listener.Stop();
            } catch (Exception ex) {
                Plugin.Log.LogWarning("dev http: stop failed: " + ex.Message);
            }
        }

        /// <summary>Drain marshaled work and evaluate /wait predicates. Runs once per frame from
        /// the pump, on the main thread.</summary>
        public void PumpMainThread() {
            while (_mainQueue.TryDequeue(out var action)) {
                action();
            }
            lock (_waitJobs) {
                for (int i = _waitJobs.Count - 1; i >= 0; i--) {
                    var job = _waitJobs[i];
                    object value = null;
                    try {
                        job.Predicate(ref value);
                    } catch (Exception ex) {
                        GameLog.Add("wait predicate threw: " + ex.Message);
                        value = null;
                    }
                    if (value is bool b && b) {
                        job.Result = true;
                        job.Done.Set();
                        _waitJobs.RemoveAt(i);
                    }
                }
            }
        }

        private void AcceptLoop() {
            while (!_stopped) {
                TcpClient client;
                try {
                    client = _listener.AcceptTcpClient();
                } catch (Exception ex) {
                    if (!_stopped) {
                        Plugin.Log.LogError("dev http: accept loop died: " + ex);
                    }
                    return;
                }
                ThreadPool.QueueUserWorkItem(_ => {
                    try {
                        Handle(client);
                    } catch (Exception ex) {
                        Plugin.Log.LogWarning("dev http: request failed: " + ex.Message);
                    } finally {
                        client.Close();
                    }
                });
            }
        }

        private void Handle(TcpClient client) {
            client.ReceiveTimeout = 30000;
            var stream = client.GetStream();
            string method, path, query, body;
            if (!ParseRequest(stream, out method, out path, out query, out body)) {
                return;
            }
            string response;
            try {
                response = Route(method, path, ParseQuery(query), body);
            } catch (Exception ex) {
                response = "error: " + ex;
            }
            byte[] payload = Encoding.UTF8.GetBytes(response ?? "");
            string header = "HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\n"
                + "Content-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private static bool ParseRequest(NetworkStream stream, out string method, out string path,
                                         out string query, out string body) {
            method = path = query = body = null;
            var buffer = new MemoryStream();
            var chunk = new byte[4096];
            int headerEnd = -1;
            while (headerEnd < 0) {
                int read = stream.Read(chunk, 0, chunk.Length);
                if (read <= 0) {
                    return false;
                }
                buffer.Write(chunk, 0, read);
                headerEnd = FindHeaderEnd(buffer);
                if (buffer.Length > 1 << 20) {
                    return false;
                }
            }
            byte[] raw = buffer.ToArray();
            string head = Encoding.ASCII.GetString(raw, 0, headerEnd);
            string[] headLines = head.Split(new[] { "\r\n" }, StringSplitOptions.None);
            string[] requestLine = headLines[0].Split(' ');
            if (requestLine.Length < 2) {
                return false;
            }
            method = requestLine[0];
            string target = requestLine[1];
            int q = target.IndexOf('?');
            path = q < 0 ? target : target.Substring(0, q);
            query = q < 0 ? "" : target.Substring(q + 1);

            int contentLength = 0;
            foreach (var line in headLines) {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) {
                    int.TryParse(line.Substring(15).Trim(), out contentLength);
                }
            }
            int bodyStart = headerEnd + 4;
            var bodyBuffer = new MemoryStream();
            bodyBuffer.Write(raw, bodyStart, raw.Length - bodyStart);
            while (bodyBuffer.Length < contentLength) {
                int read = stream.Read(chunk, 0, chunk.Length);
                if (read <= 0) {
                    break;
                }
                bodyBuffer.Write(chunk, 0, read);
            }
            body = Encoding.UTF8.GetString(bodyBuffer.ToArray());
            return true;
        }

        private static int FindHeaderEnd(MemoryStream buffer) {
            byte[] data = buffer.GetBuffer();
            for (int i = 3; i < buffer.Length; i++) {
                if (data[i - 3] == '\r' && data[i - 2] == '\n' && data[i - 1] == '\r' && data[i] == '\n') {
                    return i - 3;
                }
            }
            return -1;
        }

        private static Dictionary<string, string> ParseQuery(string query) {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in query.Split('&')) {
                if (pair.Length == 0) {
                    continue;
                }
                int eq = pair.IndexOf('=');
                if (eq < 0) {
                    result[Uri.UnescapeDataString(pair)] = "";
                } else {
                    result[Uri.UnescapeDataString(pair.Substring(0, eq))] = Uri.UnescapeDataString(pair.Substring(eq + 1));
                }
            }
            return result;
        }

        private string Route(string method, string path, Dictionary<string, string> query, string body) {
            switch (path) {
                case "/health":
                    return "ok";
                case "/speech": {
                    int since = IntParam(query, "since", 0);
                    int wait = IntParam(query, "wait", 0);
                    if (wait > 0) {
                        SpeechLog.WaitForMore(since, wait);
                    }
                    string lines = SpeechLog.Read(since, out int next);
                    return "cursor: " + next + "\n" + lines;
                }
                case "/log": {
                    int since = IntParam(query, "since", 0);
                    string lines = GameLog.Read(since, out int next);
                    if (query.TryGetValue("grep", out string needle) && needle.Length > 0) {
                        var sb = new StringBuilder();
                        foreach (var line in lines.Split('\n')) {
                            if (line.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) {
                                sb.Append(line).Append('\n');
                            }
                        }
                        lines = sb.ToString();
                    }
                    return "cursor: " + next + "\n" + lines;
                }
                case "/eval": {
                    int before = SpeechLog.Cursor;
                    string result = RunOnMain(() => _eval.Run(body), 30000);
                    return WithSpeech(result, before, query);
                }
                case "/input": {
                    string verb = body.Trim();
                    int before = SpeechLog.Cursor;
                    string result = RunOnMain(() => Drive(verb), 10000);
                    return WithSpeech(result, before, query);
                }
                case "/wait": {
                    int timeout = IntParam(query, "timeout", 10000);
                    return WaitFor(body.Trim(), timeout);
                }
                case "/nav":
                    return RunOnMain(DescribeNav, 10000);
                case "/gui":
                    return RunOnMain(GuiDump.Dump, 15000);
                case "/focus":
                    return RunOnMain(DescribeGameFocus, 10000);
                default:
                    return "unknown endpoint " + method + " " + path;
            }
        }

        private string WithSpeech(string result, int sinceCursor, Dictionary<string, string> query) {
            if (query.TryGetValue("speech", out string flag) && flag == "0") {
                return result;
            }
            int settle = IntParam(query, "settle", 250);
            int start = Environment.TickCount;
            int last = SpeechLog.Cursor;
            int lastChange = start;
            while (Environment.TickCount - lastChange < settle && Environment.TickCount - start < 3000) {
                Thread.Sleep(40);
                int cursor = SpeechLog.Cursor;
                if (cursor != last) {
                    last = cursor;
                    lastChange = Environment.TickCount;
                }
            }
            return result + "\nspeech:\n" + SpeechLog.Read(sinceCursor, out _);
        }

        private string WaitFor(string expression, int timeoutMs) {
            string error = null;
            CompiledMethod predicate = RunOnMainRaw(() => _eval.CompileExpression(expression, out error), 15000);
            if (predicate == null) {
                return "compile error: " + (error ?? "unknown");
            }
            var job = new WaitJob { Predicate = predicate, Done = new ManualResetEventSlim(false) };
            lock (_waitJobs) {
                _waitJobs.Add(job);
            }
            bool signaled = job.Done.Wait(timeoutMs);
            if (!signaled) {
                lock (_waitJobs) {
                    _waitJobs.Remove(job);
                }
                return "timeout";
            }
            return "true";
        }

        private string Drive(string verb) {
            var nav = _runtime.Router.Navigator;
            switch (verb) {
                case "up": return nav.Handle(UiActions.Up) ? "ok" : "unhandled";
                case "down": return nav.Handle(UiActions.Down) ? "ok" : "unhandled";
                case "left": return nav.Handle(UiActions.Left) ? "ok" : "unhandled";
                case "right": return nav.Handle(UiActions.Right) ? "ok" : "unhandled";
                case "confirm": return nav.Handle(UiActions.Activate) ? "ok" : "unhandled";
                case "back": return nav.Handle(UiActions.Back) ? "ok" : "unhandled";
                case "tab": return nav.Handle(UiActions.Next) ? "ok" : "unhandled";
                case "prev": return nav.Handle(UiActions.Prev) ? "ok" : "unhandled";
                case "home": return nav.Handle(UiActions.Home) ? "ok" : "unhandled";
                case "end": return nav.Handle(UiActions.End) ? "ok" : "unhandled";
                case "buffer-next": _runtime.BufferCtl.NextBuffer(); return "ok";
                case "buffer-prev": _runtime.BufferCtl.PreviousBuffer(); return "ok";
                case "buffer-item-next": _runtime.BufferCtl.NextLine(); return "ok";
                case "buffer-item-prev": _runtime.BufferCtl.PreviousLine(); return "ok";
                default: return "unknown verb " + verb;
            }
        }

        private string DescribeNav() {
            var sb = new StringBuilder();
            sb.Append(_runtime.Router.Describe()).Append('\n');
            sb.Append("gate: ").Append(_runtime.Gate.Captured ? "captured" : "released").Append('\n');
            foreach (var buffer in _runtime.Buffers.Buffers) {
                sb.Append("buffer ").Append(buffer.Key).Append(": ")
                  .Append(buffer.Position).Append('/').Append(buffer.Count)
                  .Append(buffer == _runtime.Buffers.Current ? " (current)" : "").Append('\n');
            }
            return sb.ToString();
        }

        private static string DescribeGameFocus() {
            var eventSystem = EventSystem.current;
            var selected = eventSystem == null ? null : eventSystem.currentSelectedGameObject;
            if (selected == null) {
                return "no game selection";
            }
            var path = new StringBuilder(selected.name);
            var node = selected.transform.parent;
            while (node != null) {
                path.Insert(0, node.name + "/");
                node = node.parent;
            }
            return path.ToString();
        }

        private static int IntParam(Dictionary<string, string> query, string key, int fallback)
            => query.TryGetValue(key, out string value) && int.TryParse(value, out int parsed) ? parsed : fallback;

        private string RunOnMain(Func<string> work, int timeoutMs) => RunOnMainRaw(work, timeoutMs) ?? "timeout on main thread";

        private T RunOnMainRaw<T>(Func<T> work, int timeoutMs) where T : class {
            using (var done = new ManualResetEventSlim(false)) {
                T result = null;
                _mainQueue.Enqueue(() => {
                    try {
                        result = work();
                    } catch (Exception ex) {
                        result = ("error: " + ex) as T;
                    } finally {
                        done.Set();
                    }
                });
                done.Wait(timeoutMs);
                return result;
            }
        }
    }
}
