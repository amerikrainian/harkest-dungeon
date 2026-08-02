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
        private ProfileSelectScreen _profileSelect;
        private KeyBindingsScreen _keyBindings;
        private InnScreen _inn;
        private FeedbackScreen _feedback;
        private PartyLoadoutScreen _partyLoadouts;
        private InventoryScreen _inventory;
        private InnStorageScreen _innStorage;
        private KingdomInnPanelScreen _kingdomInnPanel;
        private CombatScreen _combat;
        private AcademicScreen _academic;
        private DrivingScreen _driving;
        private string _lastTickError;
        private float _lastTickErrorTime;
        private bool _wasTyping;
        private readonly ModTextEdit _textEdit;

        public Core.Settings.ModSettings Settings { get; }
        public Core.Settings.SoundVolumes Sounds { get; }
        public Core.Input.ModKeymap Keymap { get; }

        private readonly ModRebind _rebind = new ModRebind();

        public Runtime(string pluginDir, string version, BepInEx.Configuration.ConfigFile config) {
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

            Settings = new Core.Settings.ModSettings(new Settings.BepInExSettingsStore(config));
            Sounds = new Core.Settings.SoundVolumes(new Settings.BepInExSettingsStore(config, "Sounds"));
            Keymap = new Core.Input.ModKeymap(Input, new Settings.BepInExSettingsStore(config, "Keys"),
                ParseBinding, message => Plugin.Log.LogWarning(message));
            _textEdit = new ModTextEdit(speak, () => Router.Active);

            _audioEngine = new Audio.NAudioEngine(Path.Combine(pluginDir, "assets", "audio"));
            Audio = new Core.Audio.VolumeScaledEngine(_audioEngine, Sounds);
            _roadSense = new Game.RoadSense(Audio, speak, Gate, Settings.SensingRange);
            // Eager: toasts pop on the road before any combat has resolved the lazy attach.
            Game.ToastEvents.RoadSink = _roadSense.Post;
            Game.ToastEvents.Attach();

            Router = new ScreenRouter(Navigator, Gate, speak);
            _crossroads = new CrossroadsScreen(speak);
            Router.Register(new ConfirmationScreen());
            Router.Register(new UiModalScreen());
            // The key-bindings panel overlays the settings screen's controls tab, so it must
            // outrank the settings reader.
            _keyBindings = new KeyBindingsScreen(Navigator, speak);
            Router.Register(_keyBindings);
            Router.Register(new OptionsScreen(new Screens.Options.ModTab[] {
                new Screens.Options.ModSettingsTab(Settings, _textEdit, speak),
                new Screens.Options.ModSoundsTab(Sounds, Audio, Navigator),
                new Screens.Options.ModKeysTab(Input, Keymap, _rebind, speak),
            }));
            Router.Register(new PauseScreen());
            Router.Register(new CharacterSheetScreen());
            Router.Register(new LootScreen());
            Router.Register(new StoryScreen());
            Router.Register(new BossSelectScreen());
            Router.Register(new TokenGlossaryScreen());
            Router.Register(new TutorialArchiveScreen());
            Router.Register(new PatchNotesScreen(Navigator));
            Router.Register(new InnResultsScreen());
            // The kingdom map's cell panels are stack screens over the map; the map itself
            // stands down to any pushed screen, so these register ahead of it and of the inn.
            Router.Register(new KingdomEventPanelScreen());
            _kingdomInnPanel = new KingdomInnPanelScreen(speak);
            Router.Register(_kingdomInnPanel);
            Router.Register(new KingdomBiomePanelScreen());
            _innStorage = new InnStorageScreen(speak, Navigator);
            Router.Register(_innStorage);
            Router.Register(new KingdomMapScreen(Navigator, speak, () => {
                uiBuffer.SetSource(Navigator.Current == null
                    ? (Func<System.Collections.Generic.IEnumerable<string>>)null
                    : Navigator.Current.GetBufferLines);
                Buffers.SetCurrent("ui");
            }));
            // The inn hub reads THROUGH its own inventory stack entry, so it must outrank the
            // generic floor that would otherwise take that entry.
            _inn = new InnScreen(speak, Navigator);
            Router.Register(_inn);
            // The altar's reveal modal outranks its recollection panel, which outranks the
            // floor (label-only buttons, no reveals).
            Router.Register(new AltarRevealScreen());
            Router.Register(new AltarCosmeticRevealScreen());
            Router.Register(new AltarItemScreen());
            Router.Register(new AltarCosmeticScreen());
            Router.Register(new AltarCollectionScreen());
            Router.Register(new AltarClassScreen());
            Router.Register(new AltarGeneralScreen());
            Router.Register(new AltarMemoryScreen());
            Router.Register(new AltarOptionsScreen());
            // The inn's station sub-screens, each outranking the floor's label-only sweep.
            // The store screen also serves road merchants (the Hoarder).
            Router.Register(new StoreScreen(Navigator));
            Router.Register(new MasteryScreen());
            Router.Register(new WainwrightScreen());
            Router.Register(new RelationshipMatrixScreen());
            Router.Register(new InnUpgradesScreen());
            Router.Register(new InnReplacementScreen());
            Router.Register(new RouteSelectScreen());
            // The road's node-arrival prompt, then the surfaces it opens: the Field Hospital
            // (also the inn physician; its Pharmacy tab hands off to the store screen above).
            Router.Register(new EnterNodeScreen(speak));
            Router.Register(new HospitalScreen());
            // The advance-or-escape dialog between a lair's (or guardian node's) battles.
            Router.Register(new LairAdvanceScreen(speak));
            // The standalone player inventory (road, crossroads, loot); the inn hub above
            // already took its own inline copy.
            _inventory = new InventoryScreen(speak, Navigator);
            Router.Register(_inventory);
            // The pause menu's Feedback form (legacy widgets the floor misreads).
            _feedback = new FeedbackScreen(speak);
            Router.Register(_feedback);
            // The floor for any other pushed screen (node panels) sits
            // ABOVE the mode screens: a pushed screen always covers the scene behind it.
            Router.Register(new GenericScreen());
            // The title menu's cinematics panel overlays the menu inside the same MAIN_MENU
            // mode, so it must outrank both menu readers.
            Router.Register(new CinematicsPanelScreen());
            // The profile-select panel overlays the title menu inside the same MAIN_MENU mode.
            _profileSelect = new ProfileSelectScreen(speak);
            Router.Register(_profileSelect);
            // The kingdoms scene overlays the title menu inside the same MAIN_MENU mode.
            Router.Register(new KingdomMenuScreen(speak));
            Router.Register(new MainMenuScreen());
            // The hero-select canvas overlays are not stack screens; they match off the game's
            // own panel flags and must outrank the crossroads beneath them.
            Router.Register(new PathSelectScreen());
            _partyLoadouts = new PartyLoadoutScreen(speak);
            Router.Register(_partyLoadouts);
            Router.Register(_crossroads);
            Router.Register(new EmbarkScreen());
            Router.Register(new AltarScreen());
            Router.Register(new HeroStoryIntroScreen(speak));
            // The inspector overlays the battle, so it outranks the combat floor.
            _academic = new AcademicScreen(speak);
            Router.Register(_academic);
            _combat = new CombatScreen(speak, Audio);
            Router.Register(_combat);
            Router.Register(new RouteChoiceScreen(Audio));
            // The road map shares the keyboard with live driving, so it sits below every
            // taking surface (the fork menu included). Its cursor moves re-home the buffers
            // the same way a focus change does.
            Router.Register(new MapScreen(speak, Audio, () => {
                uiBuffer.SetSource(Navigator.Current == null
                    ? (Func<System.Collections.Generic.IEnumerable<string>>)null
                    : Navigator.Current.GetBufferLines);
                Buffers.SetCurrent("ui");
            }, Input));
            // The free-driving floor: the HUD as Tab panels around a pass-through driving area.
            _driving = new DrivingScreen(speak, Navigator, Input);
            Router.Register(_driving);
            // Target-select feedback: validity beeps fire on focus landings, not per frame.
            Navigator.FocusSettled += element => {
                if (Router.Active == _combat) {
                    _combat.OnFocusSettled(element);
                }
            };

            RegisterInputs();
            Keymap.Load();
            // Keys are live while the gate holds the keyboard, and also under a screen that
            // deliberately shares it (the road map claims arrows, the game keeps WASD).
            Input.ActiveCategoriesProvider = () =>
                Gate.Captured || (Router.Active != null && !Router.Active.CapturesKeyboard) ? UiCategories : NoCategories;
            // While a game text field is being typed into, every key belongs to the field - and
            // for one tick after the edit ends: the field processes its closing Enter/Escape
            // earlier in the same frame, so that key is still this frame's press and would
            // otherwise immediately re-fire as ours (reopening the edit it just closed).
            Input.SuppressAll = () => {
                // The profile rename edit is asked for directly - the game does not report it
                // through IsInputtingText the way its other text fields are. A listening key
                // rebind pauses the same way, the game's and the mod's own alike: the pressed
                // key must become the binding.
                bool typing = Game.TextEntry.IsTyping || _textEdit.Active || _profileSelect.EditingName
                    || _keyBindings.RebindActive || _rebind.Active || _feedback.Editing
                    || _partyLoadouts.EditingName;
                bool suppress = typing || _wasTyping;
                _wasTyping = typing;
                return suppress;
            };
            Input.JustPressedDispatcher = action =>
                action.Key.StartsWith("ui.", StringComparison.Ordinal) && Router.HasScreen
                && (Router.Active.HandleAction(action.Key) || Navigator.Handle(action.Key));

            _language = new LanguageSync(Path.Combine(pluginDir, "lang"));
            Dev = DevServer.TryStart(this);

            Speech.Speak(S.ModLoaded(version));
        }

        // "pad:" entries deserialize as pad combos, everything else as keyboard - the untagged
        // keyboard form predates the pad side, so stored configs keep parsing.
        private static Core.Input.InputBinding ParseBinding(string text)
            => text.StartsWith("pad:", StringComparison.Ordinal)
                ? (Core.Input.InputBinding)PadBinding.TryDeserialize(text)
                : KeyboardBinding.TryDeserialize(text);

        private void RegisterInputs() {
            InputAction Reg(string key, string label, Action handler = null)
                => Input.Register(key, label, InputCategory.UI, handler);
            KeyboardBinding K(UnityEngine.InputSystem.Key key, bool ctrl = false, bool shift = false)
                => new KeyboardBinding(key, ctrl, shift);
            PadBinding P(PadInput input) => new PadBinding(input);

            // Pad defaults mirror say-the-spire2's layout: dpad navigates, A activates, B backs
            // out, shoulders cross panels, the right stick reviews buffers. The game's own pad
            // input is dead under a captured screen (the input gate disables its action maps),
            // so these are what makes captured screens controller-usable at all.
            Reg(UiActions.Up, S.InputNavigateUp).AddBinding(K(Key.UpArrow))
                .AddBinding(P(PadInput.DpadUp)).Repeating();
            Reg(UiActions.Down, S.InputNavigateDown).AddBinding(K(Key.DownArrow))
                .AddBinding(P(PadInput.DpadDown)).Repeating();
            Reg(UiActions.Left, S.InputNavigateLeft).AddBinding(K(Key.LeftArrow))
                .AddBinding(P(PadInput.DpadLeft)).Repeating();
            Reg(UiActions.Right, S.InputNavigateRight).AddBinding(K(Key.RightArrow))
                .AddBinding(P(PadInput.DpadRight)).Repeating();
            Reg(UiActions.Next, S.InputNextPanel).AddBinding(K(Key.Tab))
                .AddBinding(P(PadInput.RightShoulder)).Repeating();
            Reg(UiActions.Prev, S.InputPrevPanel).AddBinding(K(Key.Tab, shift: true))
                .AddBinding(P(PadInput.LeftShoulder)).Repeating();
            Reg(UiActions.Activate, S.InputActivate)
                .AddBinding(K(Key.Enter)).AddBinding(K(Key.NumpadEnter)).AddBinding(P(PadInput.A));
            Reg(UiActions.Back, S.InputBack).AddBinding(K(Key.Escape)).AddBinding(P(PadInput.B));
            Reg(UiActions.Home, S.InputJumpFirst).AddBinding(K(Key.Home));
            Reg(UiActions.End, S.InputJumpLast).AddBinding(K(Key.End));

            Reg("buffer.next", S.InputBufferNext, BufferCtl.NextBuffer)
                .AddBinding(K(Key.RightArrow, ctrl: true)).AddBinding(P(PadInput.RightStickRight));
            Reg("buffer.prev", S.InputBufferPrev, BufferCtl.PreviousBuffer)
                .AddBinding(K(Key.LeftArrow, ctrl: true)).AddBinding(P(PadInput.RightStickLeft));
            Reg("buffer.line.next", S.InputBufferLineNext, BufferCtl.NextLine)
                .AddBinding(K(Key.UpArrow, ctrl: true)).AddBinding(P(PadInput.RightStickUp)).Repeating();
            Reg("buffer.line.prev", S.InputBufferLinePrev, BufferCtl.PreviousLine)
                .AddBinding(K(Key.DownArrow, ctrl: true)).AddBinding(P(PadInput.RightStickDown)).Repeating();

            // The focused element's inspect action (the hero sheet at the crossroads and in
            // combat). C, matching the game's own "Hero Sheet (C)" hint; where the focused
            // element has no inspect, a button captioned "(C)" takes the press instead.
            Reg("ui.inspect", S.InputInspect, () => {
                if (Navigator.Current?.InvokeAction("inspect") != true && Gate.Captured) {
                    Navigator.ActivateCaptionHotkey("(C)");
                }
            }).AddBinding(K(Key.C));
            // The game captions its screen shortcuts on the buttons themselves ("Map (M)",
            // "Inventory (I)"); a captured screen swallows those keys, so the advertised key
            // presses the advertising button. On a shared-keyboard screen (driving) the game's
            // own key already fires - pressing the button too would double-toggle.
            Reg("ui.hotkey.map", S.InputHotkeyMap, () => {
                if (Gate.Captured) {
                    Navigator.ActivateCaptionHotkey("(M)");
                }
            }).AddBinding(K(Key.M));
            Reg("ui.hotkey.inventory", S.InputHotkeyInventory, () => {
                if (Gate.Captured) {
                    Navigator.ActivateCaptionHotkey("(I)");
                }
            }).AddBinding(K(Key.I));
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
            // An element advertising its own grab takes the press first (the sounds glossary's
            // loop toggle); the grab-and-place screens below never advertise one.
            if (!takeOne && Navigator.Current?.InvokeAction("grab") == true) {
                return;
            }
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
            } else if (Router.Active == _innStorage) {
                _innStorage.ToggleGrab(Navigator.Current, takeOne);
            } else if (Router.Active == _kingdomInnPanel) {
                if (!takeOne) { // heroes have no stacks to split
                    _kingdomInnPanel.ToggleGrab(Navigator.Current);
                }
            } else if (Router.Active == _driving) {
                if (!takeOne) { // heroes have no stacks to split
                    _driving.ToggleGrab(Navigator.Current);
                }
            }
        }

        public void Tick() {
            try {
                Dev?.PumpMainThread();
                _language.Tick();
                Router.Tick();
                _roadSense.Tick();
                Gate.Reassert();
                _textEdit.Tick();
                _rebind.Tick();
                // Any controller press silences ongoing speech (say-the-spire2's behavior):
                // the player acted, so whatever was being said is stale. Ahead of the input
                // tick, so an announcement the press itself causes is not the thing cut.
                if (PadBinding.AnyJustPressed()) {
                    Speech.Stop();
                }
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
