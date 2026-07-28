# Harkest Dungeon - Claude Code Instructions

**Harkest Dungeon** (namespaces and the dev env vars keep the working name DD2A11y; projects,
DLLs, the plugin folder, and the BepInEx GUID carry the public name) makes **Darkest Dungeon II**
playable by blind users. Speech is the sole interface, so if something fails silently, speaks
stale data, or omits information, the player has no way to know. A logged failure is actionable;
a silent one is invisible.

The mod owns the keyboard whenever a supported screen is up: it builds a navigable tree from the
live game UI, moves a mod-side focus with the arrow keys, and speaks each landing tersely. Detail
(tooltips, stats, descriptions) is never spoken inline - it is exploded into **buffers** the player
reviews on demand with Ctrl+arrows. Activation drives the game's own handlers (button onClick, the
screen's own methods), never synthetic OS input.

## Game & environment

- Engine: **Unity 2022.3, Mono, x64**. Game code is plain managed C# - the main assembly is
  **IronCrown.dll** (`Assembly-CSharp.dll` is nearly empty). We reference the game DLLs directly;
  no interop layer.
- Install: `Directory.Build.props` resolves `GameDir` (default Steam path with the `®` character,
  overridable via `-p:GameDir=...`, the `DD2_DIR` env var, or a gitignored
  `Directory.Build.local.props`). Steam app id **1940340**.
- Loader: **BepInEx 5.4.23 win-x64** (vendored in `third_party/bepinex/`, already installed in the
  game dir). Plugins run on the game's Mono runtime, so all projects that load in-game target
  **net472**; `HarkestDungeon.Core` targets netstandard2.0 so the net8 test project can consume it.
- UI middleware: **uGUI + TextMeshPro**, wrapped in the game's own screen framework (`UiScreenBhv`
  screens on a `ScreenStackBhv` stack, `GameModeMgr` modes for full-scene screens). Input is the
  **Unity Input System ONLY** - the legacy `UnityEngine.Input` API throws
  `InvalidOperationException` at runtime (the few game files referencing it are dead code). Our
  key reader polls `UnityEngine.InputSystem.Keyboard.current` device state directly; the game's
  own action layer is `InputSystemBhv` (action names like "Submit", "ExitMenu", "PauseMenu").
  Localization is the game's own `Localization` singleton (`GetString(locKey)`).
- Speech backend is **Prism** (https://github.com/ethindp/prism), bound via hand-written P/Invoke
  against `prism.dll`, vendored in `third_party/prism/` and deployed into the plugin folder.
- Logs: BepInEx logging with a `[Harkest Dungeon]` source into `<game>\BepInEx\LogOutput.log` (truncated
  each launch).

## Decompiled reference

`game/` (gitignored) holds the decompiled game source: `game/IronCrown/` is the game itself, plus
`Assembly-CSharp`, `RedExternal`, `LoadingSequencer`, `FMODUnity`. Look up any game type, method,
field, or loc-key usage here before guessing - this is real decompiled C# with bodies, not stubs.
Regenerate with ilspycmd **10.1** (`ilspycmd -p -o game/<name> --nested-directories -r "<Managed>"
"<Managed>/<name>.dll"`); 9.x stack-overflows on IronCrown. Most UI code lives under
`game/IronCrown/Assets/Code/ui/`.

Key game surfaces (paths relative to `game/IronCrown`):
- Screens: `Assets/Code/ui/Screens/UiScreenBhv.cs` (state machine, `m_firstSelectedGameObject`),
  stack `Assets/Code/ui/Screens/ScreenStackBhv.cs` (`GetTopMostScreenInstance`, per-layer canvases),
  modes `Assets/Code/Game/GameModeMgr.cs` + `GameModeType.cs` (MAIN_MENU, HERO_SELECT, INN, ...).
- Main menu: `Assets/Code/ui/Screens/MainMenuUiScreenBhv.cs` - not on the stack; shown by the
  MAIN_MENU mode. Serialized button fields, disclaimer flow via `OnMainMenuPress()`.
- Options: `Assets/Code/ui/Screens/OptionsMenuUiBhv.cs` (tab list `m_tabs`, rows are
  `OptionsItemBhv` toggle/slider spawned from `OptionsValue` custom-enum instances, plus bespoke
  widgets - resolution/window/language dropdowns, keybind rows).
- Pause: `Assets/Code/ui/Controllers/PauseMenuUiControllerBhv.cs` (a widget on a generic
  `UiScreenBhv` prefab, Layer.Pause; opened by `CommonUiBhv.TogglePauseMenu`).
- Crossroads (pre-run hub): the HERO_SELECT mode - `Assets/Code/Campaign/HeroSelectBhv.cs`,
  `Assets/Code/ui/EmbarkUiBhv.cs`, hero widgets `Assets/Code/ui/HeroSelect/HeroSelectActorUIBhv.cs`,
  stagecoach `Assets/Code/ui/Screens/StageCoachConfigUiBhv.cs`.
- Modals: `Assets/Code/ui/Widgets/ConfirmationDialogBhv.cs` (DataContext keys
  `confirmation_title/desc/label`, `decline_label`; spawned by `CommonUiBhv.ShowConfirmationDialog`)
  and `Assets/Code/ui/Screens/UiModalBhv.cs` (`title_text`/`body_text`).
- Tooltips: `Assets/Code/ui/Tooltips/` - `LocalizedTextTooltipBhv` (`.Text`, `m_locKey`),
  `TextTooltipBhv` (`m_text`, private), richer domain tooltips (`SkillTooltipBhv`, ...) whose text
  comes from the widget's model, not the tooltip.
- Text: labels are TMP, but most visible text is data-bound - `DataContextBhv.GetStringValue(key)`
  via `UiDisplayTextBhv` binders. Static labels carry `LocalizeTextBhv.locKey`.
- Localization: `Assets/Code/Locale/Localization.cs` - `Singleton<Localization>.Instance
  .GetString(key)` / `TryGetString(key)` (missing keys return a cyan-colored key string, so prefer
  TryGetString when a key may not exist).

## Build & deploy

`dotnet build` is the whole loop. The `HarkestDungeon` project has a post-build target (Debug only)
that copies `HarkestDungeon.dll` + `HarkestDungeon.Core.dll` + `prism.dll` + `Mono.CSharp.dll` +
`lang/*.txt` + `assets/audio` into `<GameDir>\BepInEx\plugins\HarkestDungeon\`. **Close the game
first** or the dll copy is skipped (file locked) and you'll run a stale build.

- `dotnet build HarkestDungeon.slnx -c Debug` - build all three projects and deploy (build.ps1
  wraps this with game-dir checks; setup-bepinex.ps1 installs the vendored loader first).
- `dotnet test HarkestDungeon.slnx` - run the unit suite (Core only; no game, no Unity).
- `dotnet build -c Release` compiles without deploying.

**Build Debug to test.** Only Debug deploys; `-c Release` proves compilation but leaves a stale
deployed build. There is no hot-reload on Mono - any change needs a game restart. Carry the whole
cycle yourself: kill the game, `dotnet build`, relaunch, wait for boot (~20 s to MAIN_MENU).

- Launch: `steam.exe -applaunch 1940340` (the game must go through Steam).
- Kill: `MSYS_NO_PATHCONV=1 taskkill.exe /F /IM "Darkest Dungeon II.exe"` from Bash, or
  `Stop-Process -Name 'Darkest Dungeon II'` from PowerShell.
- `tools/run-game.ps1` wraps kill + build + launch + health poll.

**Git.** Commits land on `main` by default. Only commit, merge, or push when the user asks.

## Installer

`installer/` is a standalone Windows installer (Rust; native wxWidgets GUI plus a `--cli` mode),
adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT,
https://github.com/rashadnaqeeb/NonVisualCalculus). It detects the Steam install (registry roots +
`libraryfolders.vdf`; `DD2_DIR` overrides - `detect::game_candidates` keeps a per-store framework,
Steam being the only store the game is sold on), downloads the newest `HarkestDungeon-vX.Y.Z.zip`
asset from the GitHub releases, verifies its sha256 digest, extracts it over the game dir backing
up any overwritten file, and records everything in `BepInEx/config/HarkestDungeon/install.json` so
update/repair/uninstall restore the dir exactly. Installer UI strings live in `installer/src/i18n.rs`
(English only, matching `lang/`).

- `build-installer.ps1` - cargo release build into `releases\HarkestDungeonInstaller.exe` (needs
  libclang + ninja for the wxWidgets build; both probed from Visual Studio installs).
- `test-installer.ps1` - the installer unit suite (`cargo test`; tests live in the lib target
  because the exe embeds a requireAdministrator manifest).
- `build_release.ps1` - the distributable `releases\HarkestDungeon-v<version>.zip` (vendored
  BepInEx layout + Release plugin output; the zip root is the game folder).
- `create-release.ps1 v<version>` - `gh release create` for a pushed tag with the zip + installer
  exe, notes lifted from that version's `CHANGELOG.md` section.
- End-to-end without a published release: `HARKEST_DUNGEON_INSTALLER_RELEASES_URL` points the
  installer at a locally served releases JSON, and `cargo run --release --example cli` runs the
  CLI without the exe's elevation manifest (the dev game dir is user-writable).

## Dev driver (in-process HTTP server) - for iteration, not a player feature

A loopback dev server is baked into the plugin (`Debug` builds only), on by default, binding
**127.0.0.1:8771** (`DD2A11Y_DEV_PORT` overrides; `DD2A11Y_NO_DEV=1` disables). It lets an agent
introspect and drive the live game. Bring-up: launch through Steam, then poll
`curl -s --retry 60 --retry-connrefused --retry-delay 1 http://127.0.0.1:8771/health`.

Endpoints (drive with `curl`):
- `POST /eval` - body is C# source, compiled by **Mono.CSharp** and run on the Unity main thread.
  Mono.CSharp is expression-oriented: wrap multi-statement code in
  `new System.Func<string>(() => { ...; return x; })()`; bare `return` fails. State persists
  across calls. Returns output/diagnostics/exceptions, then a `speech:` section with whatever the
  mod spoke as a consequence (waits for a quiet window; `?speech=0` skips).
- `POST /input` - body is a verb driving our own navigator via its logical handlers (never OS
  synthetic keys): `up|down|left|right|confirm|back|tab|prev|home|end` and the buffer verbs
  `buffer-next|buffer-prev|buffer-item-next|buffer-item-prev`. This is the real player path - use
  it rather than `/eval`-calling internals when testing screens.
- `POST /wait?timeout=MS` - body is a C# bool expression evaluated every frame on the main thread;
  returns when true or on timeout (default 10s). Use instead of curl sleep-loops.
- `GET /speech?since=N` - lines the mod has spoken since cursor N (we can't hear the TTS).
  `&wait=MS` long-polls. The tap is upstream of Prism, so it works with speech muted.
- `GET /log?since=N` - the BepInEx log in-band (same cursor protocol; `&grep=S` filters).
- `GET /nav` - the mod's interpreted state: active screen, focus path, buffer states.
- `GET /gui` - raw dump of the active uGUI hierarchy (paths, components, TMP text, DataContext
  values). Diff against `/nav` to find where the mod loses information.
- `GET /focus` - the game's own EventSystem selection, independent of our navigator.
- `GET /health` - liveness.

Headless runs: `DD2A11Y_NO_SPEECH=1` skips Prism init (spoken text still captured for `/speech`),
so an unattended session doesn't depend on a running screen reader.

## Architecture

Three projects:

- **`HarkestDungeon.Core`** (netstandard2.0, namespace `DD2A11y.Core`) - engine-agnostic logic: the speech pipeline, text filter,
  the authored-strings table + translations, the navigator/container model, the input registry,
  and the **buffer system**. References nothing external (no Unity, no BepInEx) so it stays
  unit-testable off-engine. If a piece of code decides what words the user hears, it belongs here.
- **`HarkestDungeon`** (net472, namespace `DD2A11y`) - the BepInEx plugin: entry (`Plugin`), the Prism P/Invoke backend, the
  one pump MonoBehaviour, the input gate, the dev server, and the game-coupled side of every
  screen: adapters that read live game state and screen classes that build navigable trees.
- **`HarkestDungeon.Tests`** (net8.0 + xUnit) - references Core only. No Unity, no game launch.

**Screen model.** `ScreenRouter` (plugin) resolves the active surface once per frame, in priority
order: topmost modal (a live `ConfirmationDialogBhv`/`UiModalBhv`) -> topmost supported
`ScreenStackBhv` screen (options, pause) -> the current `GameModeMgr` mode's screen (main menu,
crossroads). When a `GameScreen` matches, the router takes the keyboard (input gate), builds the
screen's tree fresh (`BuildRoot`), attaches the `TraditionalNavigator`, and speaks the screen name
then the landing. When it stops matching, the keyboard is released. A screen's tree is **built
fresh on entry and read live** - elements hold references to live game components and read them at
speech time.

**Navigation model** (Core, engine-free): `UIElement` leaves (Label/Role/Value read live,
`ElementAction`s invoked by id) inside `Container`s (VerticalList/HorizontalList/Panel);
`TraditionalNavigator` owns the focus path - arrows move within lists, Left/Right adjust a focused
slider/stepper (advertised increase/decrease actions), Tab/Shift-Tab cross panels, Enter activates,
Escape asks the screen root's back action, Home/End jump. Tabbed screens (options) put a
`TabSelector` element first in a vertical list: Left/Right on it switch tabs (rebuilding the items
below it), Up/Down walk from the tab header through the active tab's items, and the screen
remembers its last tab across close/reopen.

**Buffer model** (Core): a `Buffer` is a named flat list of text lines with a cursor (`ui` first;
more to come - `hero`, `events`). On every focus change the plugin resets all buffers and the
focused element populates them: the `ui` buffer gets the element's own line first, then **one line
per tooltip**. Ctrl+Left/Right switch among non-empty buffers (speaks "name: current line"),
Ctrl+Up/Down step lines (speaks the line). Buffers repopulate from the live element on every
buffer keypress, so they never go stale.

**Adapter / composition split.** Reading live game state touches Unity and lives in a thin adapter
in the plugin that extracts raw state into plain data (no Unity types past the boundary) and does
no formatting. The spoken line is composed from that data by Core, which is unit-tested.

**Announce from the pump.** The single pump MonoBehaviour drives everything once per frame: input
tick -> router resolve -> screen update -> navigator dispatch. Harmony patches and game events only
record state or set dirty flags; speech happens in the pump path.

## Conventions & invariants

**Speech, logging & input**
- All speech goes through `SpeechPipeline` (`DD2A11y.Core.Speech.SpeechPipeline`); never call the
  Prism backend directly. All logging goes through `Plugin.Log` (the BepInEx `ManualLogSource`),
  never Unity's `Debug.Log`.
- Never interrupt existing speech unless an action supersedes it (navigation). Default to queued.
- Activation drives the game's own logic (onClick.Invoke(), the screen's own public methods),
  never synthesized clicks or OS key events.

**Tooltips & detail: buffers, never inline.** The focus announcement is terse - label, role,
value ("Continue, button"). Anything longer an object carries - tooltips (nested ones too), stat
blocks, descriptions - is collected into buffer lines at focus time, one line per tooltip/detail,
so the player reads it on demand with Ctrl+arrows. Never append tooltip text to the focus line.

**Modals.** Every modal reads as: the text (title + body as the first, focusable element), then
each choice, all reachable with Up/Down. A modal's appearance interrupts and announces itself; its
dismissal must be spoken too (the underlying screen re-announces).

**State & strings**
- **Never cache game state.** Do not copy game data into mod-side dictionaries, lists, or string
  fields for later use; re-query the game when you need a value. The only acceptable "cache" is a
  reference to a live Unity component read at speech time. When several callers read the same game
  model, centralize reads in one adapter class.
- **Reuse game data, avoid hardcoding.** Use the game's own localized text wherever possible: a
  label's `LocalizeTextBhv.locKey` or bound `DataContextBhv` value, a tooltip's loc key, option
  names from `OptionsValue.m_locKey` - all through `Localization.GetString`/`TryGetString`.
  Hardcoded text goes stale across game updates and blocks translation. Before authoring any
  string, first check whether a game string could be used.
- **No inline user-facing string literals.** Every word the mod itself authors and speaks comes
  from the central strings table in Core (`DD2A11y.Core.Strings.Strings`), never an inline
  literal. Punctuation and log/debug text are exempt. The table is runtime-translatable: each
  string is a key with an English default, and a `lang/<language>.txt` file overrides values per
  key, missing keys falling back to English. Word order lives in `{0}`-style templates and plurals
  in `|`-separated forms - never concatenate English grammar around a value in code; add a
  template or plural key instead. `lang/en.txt` is the generated translator template; a test pins
  it to `Strings.DumpTemplate()` (on failure, regenerate by writing that string to the file).

**Announcements (mod-authored text only - never reword game text).** Users are expert
screen-reader users; strip fluff, never information.
- Distinguishing word first: the sooner the varying part appears, the faster the user moves on.
- No positional counts ("3 of 10") - the reader tracks position. No nav hints unless an unusual
  control, and on a delay. No redundant context or obvious type suffixes.
- Include all gameplay-relevant detail; concise means no fluff, not less information. Avoid
  em-dashes (announced as "dash") and fancy punctuation.
- Label a section/readout for what it is, reusing the game's own header string when one exists.

**No silent failures.** The mod runs on a per-frame pump and reflection against game internals,
which fail invisibly unless logged. Every catch logs via `Plugin.Log.LogWarning`/`LogError` what
failed and where. No empty catches, no catch-and-return-default without logging. Reflection
lookups (a private field read, a type by name) are resolved once at startup through
`GameAccess` helpers that log loudly when the game's shape changed.

**Docs.** `docs/accessibility_audit.md` tracks per-screen status (what reads, what doesn't, known
gaps). Update it in the same change that adds or fixes a screen.

## Gotchas

Recurring traps, several inherited from the previous incarnation of this mod - they will bite
again on each new screen.

- **`/input` bypasses the physical key path.** The dev verb drives the navigator's logical
  handlers directly, so it proves screen logic but NOT key polling - that is how a broken
  `KeyboardBinding` shipped while every scripted test passed. To exercise the real path, inject
  device-level events via `/eval`: `InputSystem.QueueStateEvent(Keyboard.current, new
  KeyboardState(Key.DownArrow))` (then an empty `KeyboardState()` to release), or press keys.
- **The game destroys the BepInEx manager object during boot**, taking the plugin component's
  `OnDestroy` with it. Never dispose mod state there (the dev server dies silently); dispose only
  on `Application.quitting`. Everything long-lived hangs off the mod's own hidden pump object.
- **`UiScreenBhv.GoBack()` returning true means "proceed to close"** (`CommonUiBhv` then calls
  `TryCloseScreen`); false means the screen consumed Escape as an internal step. The options
  screen's Escape is two-stage on mouse+keyboard (deselect tab, then close) - the mod folds both
  into one press.
- **The options screen's open sequence stomps the tab to 0 after our entry** - enforce the
  remembered tab every frame until `ScreenState == Open`, then request a re-announce if the entry
  announcement read the wrong tab.
- **Icon-only buttons carry their caption only in a tooltip** (`LocalizedTextTooltipBhv` on the
  main-menu footer buttons) - `UiText.FirstLabel` falls back to the first tooltip line, and the
  buffer dedupes it.
- **Decorative selectables have only inactive placeholder text** (the pause menu's profile badge:
  "Player Username") - `UiText.HasAnyTextSource` (active-only) filters them out of screens.
- **The main-menu disclaimer blocks all buttons** until `MainMenuUiScreenBhv.OnMainMenuPress()`
  (the AnyKey handler) runs. uGUI clicks cannot dismiss it; call the method.
- **The game re-enables `sendNavigationEvents`** in its own dialog teardown, so the input gate
  must re-assert its capture state every frame it owns the keyboard, not just on acquire.
- **`SelectOnEmptyFallbackBhv` marks invisible selection anchors** - exclude those objects when
  sweeping a screen for navigable widgets.
- **Some widgets activate on uGUI SELECTION, not click** (their `OnSelect` drives game state). Do
  not mirror our focus into `EventSystem.SetSelectedGameObject` for such widgets while browsing;
  activate must drive the game's own view method instead, so browsing stays side-effect-free.
- **DataContext binders apply text a frame late.** On a commit/act, speak from the model or loc
  keys, not from the TMP text that will only update next frame.
- **Options prefab templates sit offscreen but activeInHierarchy** (`OptionsItemBhv` with
  `OptionValue == null`) - skip them. Inactive TMP children hold placeholder text ("Tooltip
  Text"); read active-only text.
- **Boot to MAIN_MENU takes ~20 s** after launch. The dev server comes up earlier; `/wait` on
  `GameModeMgr.CurrentMode` rather than sleeping.
- **ilspycmd 9.x stack-overflows on IronCrown**; use 10.1 (installed).
- **`/eval` is Mono.CSharp**: expression form only - wrap statements in an invoked
  `Func<string>` lambda. Its 2015-era compiler also ICEs on nested generic types
  (`List<Outer.Nested>`) - use `System.Collections.IList` plus reflection in eval snippets. The
  host must not `ReferenceAssembly` mscorlib/System/System.Core/System.Xml (the evaluator loads
  its own defaults) nor the netstandard/System.Runtime facades, or every System type is
  ambiguous.
- **Pooled lists recycle EVERY widget on re-populate** (`PooledListBhv` - inventory slots on
  sort, station buttons on inn-state changes): the same count of brand-new instances replaces
  the old ones, so a count-based rebuild check reads equal while every held reference is dead.
  Key rebuild checks to an instance-id signature over the swept widgets, and keep elements
  over persistent widgets out of the rebuilt container so focus survives.
- **FMOD owns audio.** Whether a mod-owned Unity `AudioSource` is audible is unverified; check
  before building audio cues.

## Common LLM Antipatterns

### Comments and docs: state what is, not what isn't
Comments and documentation describe the current state and why - not the change history, the
absence of something, or a path not taken. Consider whether a comment is needed at all.

**WRONG**: `// Removed the old UI system. Now x does y.`
**WRONG**: `// We don't use the game's Navigate action` (documents a non-thing)
**CORRECT**: `// Can be closed with the controller`

This governs descriptive text. Prescriptive rules and API contracts ("never call the backend
directly") and a what-happens fact that justifies an instruction are fine.

### Defensive null handling
Excessive validation hides bugs. Only null-check where null is a legitimate, expected state (e.g.,
after `FirstOrDefault()`, at reflection boundaries against the game). Let code crash otherwise - a
crash is visible and logged by the pump's catch; a silently swallowed null is not. Trust private
callers.

### No throwaway dev hacks
Never hack a temporary bypass into the tree to dodge a proper rebuild or restart (e.g.
`if (_devEnabled || true)`). The rebuild+relaunch loop is cheap and meant to be used. Keep gates
honest.
