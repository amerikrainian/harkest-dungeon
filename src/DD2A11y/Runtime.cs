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
using Key = UnityEngine.InputSystem.Key;
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
        public Core.Audio.IAudioEngine Audio { get; }
        public DevServer Dev { get; }

        private readonly PrismBackend _backend;
        private readonly Audio.NAudioEngine _audioEngine;
        private readonly Game.RoadSense _roadSense;
        private readonly LanguageSync _language;
        private CrossroadsScreen _crossroads;
        private InnScreen _inn;
        private InventoryScreen _inventory;
        private CombatScreen _combat;
        private AcademicScreen _academic;
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
            // The battle-event history; filled from the combat screen's pump path and empty
            // outside combat (an empty buffer is skipped by the review keys).
            var combatBuffer = Buffers.Add(new Core.Buffers.Buffer("combat", () => S.BufferCombat));
            combatBuffer.FollowLatest = true;
            combatBuffer.SetSource(Game.CombatLog.Lines);

            Core.Text.SpriteText.Resolver = Game.SpriteWords.Resolve;

            _audioEngine = new Audio.NAudioEngine(Path.Combine(pluginDir, "assets", "audio"));
            Audio = _audioEngine;
            _roadSense = new Game.RoadSense(Audio, speak, Gate);

            Router = new ScreenRouter(Navigator, Gate, speak);
            _crossroads = new CrossroadsScreen(speak);
            Router.Register(new ConfirmationScreen());
            Router.Register(new UiModalScreen());
            Router.Register(new OptionsScreen());
            Router.Register(new PauseScreen());
            Router.Register(new CharacterSheetScreen());
            Router.Register(new LootScreen());
            Router.Register(new StoryScreen());
            Router.Register(new BossSelectScreen());
            Router.Register(new InnResultsScreen());
            // The inn hub reads THROUGH its own inventory stack entry, so it must outrank the
            // generic floor that would otherwise take that entry.
            _inn = new InnScreen(speak, Navigator);
            Router.Register(_inn);
            // The altar's reveal modal outranks its recollection panel, which outranks the
            // floor (label-only buttons, no reveals).
            Router.Register(new AltarRevealScreen());
            Router.Register(new AltarRecollectionScreen());
            Router.Register(new AltarOptionsScreen());
            // The inn's station sub-screens, each outranking the floor's label-only sweep.
            // The store screen also serves road merchants (the Hoarder).
            Router.Register(new StoreScreen(Navigator));
            Router.Register(new MasteryScreen());
            Router.Register(new WainwrightScreen());
            Router.Register(new RouteSelectScreen());
            // The standalone player inventory (road, crossroads, loot); the inn hub above
            // already took its own inline copy.
            _inventory = new InventoryScreen(speak, Navigator);
            Router.Register(_inventory);
            // The floor for any other pushed screen (glossary, node panels) sits
            // ABOVE the mode screens: a pushed screen always covers the scene behind it.
            Router.Register(new GenericScreen());
            Router.Register(new MainMenuScreen());
            Router.Register(_crossroads);
            Router.Register(new EmbarkScreen());
            Router.Register(new AltarScreen());
            // The inspector overlays the battle, so it outranks the combat floor.
            _academic = new AcademicScreen(speak);
            Router.Register(_academic);
            _combat = new CombatScreen(speak, Audio);
            Router.Register(_combat);
            Router.Register(new RouteChoiceScreen(Audio));
            // Target-select feedback: validity beeps fire on focus landings, not per frame.
            Navigator.FocusSettled += element => {
                if (Router.Active == _combat) {
                    _combat.OnFocusSettled(element);
                }
            };

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
            KeyboardBinding K(UnityEngine.InputSystem.Key key, bool ctrl = false, bool shift = false)
                => new KeyboardBinding(key, ctrl, shift);

            Reg(UiActions.Up, S.InputNavigateUp).AddBinding(K(Key.UpArrow)).Repeating();
            Reg(UiActions.Down, S.InputNavigateDown).AddBinding(K(Key.DownArrow)).Repeating();
            Reg(UiActions.Left, S.InputNavigateLeft).AddBinding(K(Key.LeftArrow)).Repeating();
            Reg(UiActions.Right, S.InputNavigateRight).AddBinding(K(Key.RightArrow)).Repeating();
            Reg(UiActions.Next, S.InputNextPanel).AddBinding(K(Key.Tab)).Repeating();
            Reg(UiActions.Prev, S.InputPrevPanel).AddBinding(K(Key.Tab, shift: true)).Repeating();
            Reg(UiActions.Activate, S.InputActivate)
                .AddBinding(K(Key.Enter)).AddBinding(K(Key.NumpadEnter));
            Reg(UiActions.Back, S.InputBack).AddBinding(K(Key.Escape));
            Reg(UiActions.Home, S.InputJumpFirst).AddBinding(K(Key.Home));
            Reg(UiActions.End, S.InputJumpLast).AddBinding(K(Key.End));

            Reg("buffer.next", S.InputBufferNext, BufferCtl.NextBuffer)
                .AddBinding(K(Key.RightArrow, ctrl: true));
            Reg("buffer.prev", S.InputBufferPrev, BufferCtl.PreviousBuffer)
                .AddBinding(K(Key.LeftArrow, ctrl: true));
            Reg("buffer.line.next", S.InputBufferLineNext, BufferCtl.NextLine)
                .AddBinding(K(Key.UpArrow, ctrl: true)).Repeating();
            Reg("buffer.line.prev", S.InputBufferLinePrev, BufferCtl.PreviousLine)
                .AddBinding(K(Key.DownArrow, ctrl: true)).Repeating();

            // The focused element's inspect action (the hero sheet at the crossroads and in
            // combat). C, matching the game's own "Hero Sheet (C)" hint.
            Reg("ui.inspect", S.InputInspect, () => Navigator.Current?.InvokeAction("inspect"))
                .AddBinding(K(Key.C));
            // The combat inspector (the game's academic view): I toggles it on the focused
            // combatant; while it is up, A/D cycle combatants - the game's own keys for it.
            Reg("combat.inspector", S.InputInspector, () => _academic.Toggle(Router, Navigator))
                .AddBinding(K(Key.I));
            Reg("combat.inspector.prev", S.InputInspectorPrev, () => _academic.Cycle(Router, -1))
                .AddBinding(K(Key.A));
            Reg("combat.inspector.next", S.InputInspectorNext, () => _academic.Cycle(Router, +1))
                .AddBinding(K(Key.D));
            // Discard the focused item (the game's shift-click); the element advertises the
            // action only where the game allows the discard, so anything else answers
            // "unavailable" rather than silence.
            Reg("ui.discard", S.InputDiscard, () => {
                if (Navigator.Current == null || !Navigator.Current.InvokeAction("discard")) {
                    Speech.Speak(S.StatusUnavailable, interrupt: true);
                }
            }).AddBinding(K(Key.Enter, shift: true)).AddBinding(K(Key.NumpadEnter, shift: true));
            // Grab-and-place: hero moves at the crossroads, inventory stacks at the inn - one
            // key, routed by what stands under focus. Shift+Space never initiates; it places
            // a single item off the held stack, repeatable until the stack runs out.
            Reg("ui.grab", S.InputGrab, () => ToggleGrab(takeOne: false)).AddBinding(K(Key.Space));
            Reg("ui.place.one", S.InputPlaceOne, () => ToggleGrab(takeOne: true))
                .AddBinding(K(Key.Space, shift: true));
        }

        private void ToggleGrab(bool takeOne) {
            if (Navigator.Current is Elements.HeroSlotElement) {
                if (!takeOne) { // heroes have no stacks to split
                    _crossroads.ToggleGrab(Navigator.Current);
                }
                return;
            }
            if (Router.Active == _inn) {
                _inn.ToggleGrab(Navigator.Current, takeOne);
            } else if (Router.Active == _inventory) {
                _inventory.ToggleGrab(Navigator.Current, takeOne);
            }
        }

        public void Tick() {
            try {
                Dev?.PumpMainThread();
                _language.Tick();
                Router.Tick();
                _roadSense.Tick();
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
            _audioEngine.Dispose();
            _backend.Shutdown();
        }
    }
}
