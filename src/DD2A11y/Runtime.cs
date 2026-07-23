using System;
using System.IO;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Input;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Dev;
using DD2A11y.Input;
using DD2A11y.Screens;
using DD2A11y.Speech;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y {
    /// <summary>Owns every subsystem and the per-frame order: dev-server pump, language sync,
    /// screen routing, input-gate re-assert, then key dispatch.</summary>
    public sealed class Runtime : IDisposable {
        private static readonly InputCategory[] UiCategories = { InputCategory.UI };
        private static readonly InputCategory[] NoCategories = { };

        public SpeechPipeline Speech { get; }
        public InputManager Input { get; } = new InputManager();
        public TraditionalNavigator Navigator { get; }
        public BufferManager Buffers { get; } = new BufferManager();
        public BufferControls BufferCtl { get; }
        public ScreenRouter Router { get; }
        public InputGate Gate { get; } = new InputGate();
        public DevServer Dev { get; }

        private readonly PrismBackend _backend;
        private readonly LanguageSync _language;
        private string _lastTickError;
        private float _lastTickErrorTime;

        public Runtime(string pluginDir, string version) {
            _backend = new PrismBackend();
            if (Environment.GetEnvironmentVariable("DD2A11Y_NO_SPEECH") == "1") {
                Plugin.Log.LogInfo("speech: skipped (DD2A11Y_NO_SPEECH=1)");
            } else {
                _backend.Initialize(Path.Combine(pluginDir, "prism.dll"));
            }
            Speech = new SpeechPipeline(_backend);
            SpeechPipeline.Instance = Speech;

            Action<string, bool> speak = (text, interrupt) => Speech.Speak(text, interrupt);
            Navigator = new TraditionalNavigator(speak);
            BufferCtl = new BufferControls(Buffers, speak);

            var uiBuffer = Buffers.Add(new Core.Buffers.Buffer("ui", () => S.BufferControl));
            Navigator.FocusSettled += element => {
                uiBuffer.SetSource(element == null ? (Func<System.Collections.Generic.IEnumerable<string>>)null
                                                   : element.GetBufferLines);
                Buffers.SetCurrent("ui");
            };

            Router = new ScreenRouter(Navigator, Gate, speak);
            Router.Register(new ConfirmationScreen());
            Router.Register(new UiModalScreen());
            Router.Register(new OptionsScreen());
            Router.Register(new PauseScreen());
            Router.Register(new MainMenuScreen());
            Router.Register(new CrossroadsScreen());

            RegisterInputs();
            Input.ActiveCategoriesProvider = () => Gate.Captured ? UiCategories : NoCategories;
            Input.JustPressedDispatcher = action =>
                action.Key.StartsWith("ui.", StringComparison.Ordinal) && Router.HasScreen && Navigator.Handle(action.Key);

            _language = new LanguageSync(Path.Combine(pluginDir, "lang"));
            Dev = DevServer.TryStart(this);

            Speech.Speak(S.ModLoaded(version));
        }

        private void RegisterInputs() {
            InputAction Reg(string key, string label, Action handler = null)
                => Input.Register(key, label, InputCategory.UI, handler);
            KeyboardBinding K(KeyCode key, bool ctrl = false, bool shift = false)
                => new KeyboardBinding(key, ctrl, shift);

            Reg(UiActions.Up, S.InputNavigateUp).AddBinding(K(KeyCode.UpArrow)).Repeating();
            Reg(UiActions.Down, S.InputNavigateDown).AddBinding(K(KeyCode.DownArrow)).Repeating();
            Reg(UiActions.Left, S.InputNavigateLeft).AddBinding(K(KeyCode.LeftArrow)).Repeating();
            Reg(UiActions.Right, S.InputNavigateRight).AddBinding(K(KeyCode.RightArrow)).Repeating();
            Reg(UiActions.Next, S.InputNextPanel).AddBinding(K(KeyCode.Tab)).Repeating();
            Reg(UiActions.Prev, S.InputPrevPanel).AddBinding(K(KeyCode.Tab, shift: true)).Repeating();
            Reg(UiActions.Activate, S.InputActivate)
                .AddBinding(K(KeyCode.Return)).AddBinding(K(KeyCode.KeypadEnter));
            Reg(UiActions.Back, S.InputBack).AddBinding(K(KeyCode.Escape));
            Reg(UiActions.Home, S.InputJumpFirst).AddBinding(K(KeyCode.Home));
            Reg(UiActions.End, S.InputJumpLast).AddBinding(K(KeyCode.End));

            Reg("buffer.next", S.InputBufferNext, BufferCtl.NextBuffer)
                .AddBinding(K(KeyCode.RightArrow, ctrl: true));
            Reg("buffer.prev", S.InputBufferPrev, BufferCtl.PreviousBuffer)
                .AddBinding(K(KeyCode.LeftArrow, ctrl: true));
            Reg("buffer.line.next", S.InputBufferLineNext, BufferCtl.NextLine)
                .AddBinding(K(KeyCode.UpArrow, ctrl: true)).Repeating();
            Reg("buffer.line.prev", S.InputBufferLinePrev, BufferCtl.PreviousLine)
                .AddBinding(K(KeyCode.DownArrow, ctrl: true)).Repeating();
        }

        public void Tick() {
            try {
                Dev?.PumpMainThread();
                _language.Tick();
                Router.Tick();
                Gate.Reassert();
                Input.Tick(Time.unscaledTimeAsDouble);
            } catch (Exception ex) {
                // Log loudly but without flooding: a fault here repeats every frame.
                if (ex.Message != _lastTickError || Time.unscaledTime - _lastTickErrorTime > 5f) {
                    _lastTickError = ex.Message;
                    _lastTickErrorTime = Time.unscaledTime;
                    Plugin.Log.LogError("tick failed: " + ex);
                }
            }
        }

        public void Dispose() {
            Dev?.Dispose();
            _backend.Shutdown();
        }
    }
}
