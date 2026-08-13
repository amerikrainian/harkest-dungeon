# Harkest Dungeon: Comprehensive Accessibility Audit

## Every User-Facing Screen and Surface in Darkest Dungeon II

*Ordered by Encounter Sequence*

> **Note:** This document catalogues every screen, panel, overlay, modal, and mode surface a
> player encounters in Darkest Dungeon II (Confessions and Kingdoms), and the accessibility
> status of each under the Harkest Dungeon mod. It is organized in the order a player first
> encounters each surface, progressing from launch through a confession run, the inn loop, and
> Kingdoms. Update it in the same change that adds or fixes a screen.

> **Status key:** **WORKS** = live-verified in the running game (a WORKS section may still list
> known gaps). **PARTIAL** = the core flow is live-verified but named parts are not.
> **BUILT** = deployed; code complete but not yet live-verified, or verified only synthetically.
> **NOT STARTED** = generic-floor reading at best, or dead air. **N/A** = nothing to make
> accessible.

> **Names:** mod screen classes appear as (`MainMenuScreen`); game classes as
> (`MainMenuUiScreenBhv`) where the game's shape matters.

> **Focus line order:** status words lead ("selected", "owned", "mastered", "blessed",
> "in party", a toggle's on/off, "unavailable"), then label, role, and value (a slider's
> percent, a dropdown's choice, costs). Refusal reasons (the game's insufficient-funds or
> invalid-skill wording) trail as part of the value.

---

# Phase 0: Shared Interaction Model

Conventions every supported screen follows. Individual sections below note only departures.

- Arrows move the mod's own focus; Enter activates; Escape backs out; Home/End jump;
  Tab/Shift-Tab cross panels.
- Focus lines are terse (label, role, value). Tooltips and detail are buffer lines:
  Ctrl+Up/Down step lines, Ctrl+Left/Right switch buffers. Buffers repopulate live on every
  buffer keypress, so they never go stale.
- The buffer roster, in cycling order: control (the focused element's own lines), mastery
  (the focused skill's upgrade preview, named by the game's own upgrade header; "no upgrade
  available" on a skill with nothing left to preview), hero (the vitals of the hero the
  focused element concerns - a skill's owner, a story choice's hero, the sheet's paged hero),
  enemies and party (one overview line per combatant, combat only), combat (the battle
  log), and subtitles (the on-screen subtitle history, named by the game's own Subtitles
  option label; lines speak as they appear and collect only while that setting is on, so
  the buffer exists only with subtitles enabled). Empty buffers are skipped, so each
  surface cycles only the buffers that answer there.
- Modals read their text first, then each choice, all on Up/Down. A modal announces itself on
  appearance; its dismissal is spoken too (the underlying screen re-announces).
- **Screens under the transition veil stay silent** (live-verified 2026-08-09 on Continue
  Confession into an inn): through a game-mode change and while the screen fader's curtain
  or wipe is up, stack screens and the generic floor read nothing - the game pushes screens
  under the curtain while assembling the next mode (the inn's inventory panel used to read
  "Inventory, All Items, tab" mid-load), and a screen nobody saw must not read. The gate is
  `StackTop.Veiled`; modals bypass it, so a dialog interrupting a transition still speaks.
- Tabbed screens put the tab selector first: Left/Right switch tabs, Down enters the tab's
  items, and the screen remembers its tab across close/reopen.
- **Advertised hotkeys work on captured screens** (live-verified 2026-07-25 on a road story):
  the game captions its shortcuts on the buttons themselves ("Map (M)", "Inventory (I)",
  "Hero Sheet (C)"), and the input gate swallows those keys - M, I, and C activate the button
  carrying that caption in the current tree, through its own onClick. C first tries the focused
  element's inspect action (a hero), then the "(C)" button. The handlers stand down whenever
  the gate is not captured, so shared-keyboard screens never double-fire the game's own keys.
- **Space is the keyboard face of the game's drag-and-drop** wherever the game drags: hero
  slots at the crossroads, inventory stacks (with Shift+Space single-item splits), garrison
  order on the kingdom map, marching order on the road.
- **Inline effect glyphs in game text speak as words**, pipeline-wide: token and dot names
  resolve through the game's own `token_name_*` / `dot_name_*` strings; the icons with no name
  string anywhere in the game (heal, buff, debuff, stress, disease, speed, HP, deathblow)
  carry authored words; any other icon speaks its humanized sprite name rather than silently
  dropping ("-2 speed" on a trinket, not a bare "-2"). Known-decorative glyphs (the hero-seal
  mark) are the only ones dropped. (Fixed 2026-08-12: the deathblow-resist glyph
  `icon_death_outline` humanized to "death", so Toe to Toe's mastered Ravager rider read
  "+4% death" where the icon means deathblow RES; it now reads "+4% deathblow" on the card
  and "+4% deathblow RES" where the game's buff strings append their own "RES".)
- **The game's "???" placeholder reads as "unknown"**: a free-standing run of question marks (a
  locked confession's name, "Rewards: ???") is voiced as NOTHING by synthesizers; the text
  filter speaks it as the authored word "unknown", pipeline-wide. Runs attached to a word
  ("What???" in a bark) keep their marks. Live-verified 2026-07-24 on the confession select.

---

# Phase 1: Launch and Title Menus

## 1.1 Boot and Loading - N/A

Steam launch to MAIN_MENU takes ~20 s; the LOADING mode surface is non-interactive. Nothing to
read.

## 1.2 Main Menu (`MainMenuScreen`) - WORKS

Live-verified 2026-07-27; visual-order fix user-caught and verified 2026-07-31. The title menu
(`MainMenuUiScreenBhv`) is not on the screen stack; it is shown by the MAIN_MENU mode.

- Disclaimer text + continue control (drives `OnMainMenuPress` - the AnyKey handler; uGUI
  clicks cannot dismiss it), then the buttons in VISUAL order: Confessions, Kingdoms, Origin
  Pack, Mods, then Exit Game / Mailing List / Patch Notes / Cinematics left to right, the
  profile button (the bottom-right journal art, prefab name `Journalbutton`) last.
- The profile button labels itself with the CURRENT PROFILE'S NAME (its TMP text), so it
  reads name first with the game's "Change Profile" tooltip as its value ("Darkest, button,
  Change Profile" - live-verified 2026-08-02); it opens the profile select panel (1.7).
- Confessions submenu (a container swap the count-rebuild picks up): Continue Confession, New
  Confession.
- Icon-only footer buttons (Exit Game, Patch Notes, Cinematics, Mailing List) read via their
  tooltip; the Confessions/Kingdoms tooltips land in the buffer.
- Escape opens settings (the game's own Escape behavior at the title).

> **Note:** The serialized selectable list groups the footer first, so a naive sweep read the
> menu upside down (Exit Game first). The sweep sorts by screen position: rows top to bottom,
> left to right within a row, grouping by a quarter of a button's world height - the footer
> buttons sit 3 px apart in Y, one row; the Origin Pack side promo rides 25 px below Kingdoms
> and must not merge into its row.

> **Note:** Through the open animation (after the disclaimer press, or entering the menu
> mid-pan) the game holds every button disabled with its tooltip caption off and unlocks them
> staggered across frames. The tree holds until the landing button is interactable and labeled
> (a changed order signature rebuilds only after holding two frames; elements are reused per
> button, so the settle re-sorts silently, and the Confessions submenu swap - which deactivates
> the main stack a frame before its own buttons arrive - re-homes onto "Continue Confession").
> The press is followed by silence, then one clean landing. A keypress during the animation
> skips it, the game's own behavior.

**Known gaps:** the profile button reads the profile name (its "Change Profile" caption is a
buffer line); the entry landing can speak the transient phase once before settling (the game
briefly presents the continue state on boot).

## 1.3 Watch Cinematics Panel (`CinematicsPanelScreen`) - WORKS

Live-verified 2026-07-28. A timeline-animated panel on the menu itself (no stack entry; it
locks every menu button while up - previously it read as the stale menu tree, bare unavailable
buttons).

- A dedicated screen outranks both menu readers while `IsCinematicPanelActive()`: named from
  the game's own title, one vertical list of the unlocked cinematic buttons then the panel's
  Back. The takeover holds until the landing button is interactable and labeled (the open
  timeline fades the buttons in).
- Escape closes through the game's own `CloseCinematicPanel`; the menu re-announces once its
  close animation unlocks the buttons.
- Playing a cinematic switches the game to CINEMATIC mode: the screen stands down and the
  keyboard is released for the game's own skip flow (any key shows the skip dialog, holding
  Space for a second skips - device-verified). On the revert the game itself has closed the
  panel, so the menu re-announces.

**Known gaps:** the End cinematic button (unlocked by a "body" boss victory) is swept
generically but was inactive on the dev profile, so it is unverified.

## 1.4 Cinematic Playback (CINEMATIC mode) - WORKS

The mod stands down and releases the keyboard; the game's own skip flow (any key, hold Space)
is fully keyboard-usable. Device-verified alongside 1.3.

- Subtitles speak as each line appears, from the pump (no screen stands during the video).
  Both subtitle surfaces funnel through `SubtitlesUtils.TryUpdateDisplay` - the cinematic
  manager's timed video lines and the general manager's in-run narration lines - and the
  spoken gate is the game's own `ShouldSubtitleBeVisible` (the Subtitles toggle in the game
  options tab plus its dev-pref overrides), so what fires on screen is what is spoken.
- The lines collect into the subtitles buffer (the session's full transcript, cursor
  following the latest), reviewable wherever the mod next owns the keyboard; while the
  Subtitles setting is off the buffer reads empty and drops out of the cycle.

## 1.5 Kingdoms Entry, Save Select, and Creation Wizard (`KingdomMenuScreen`) - WORKS

Live-verified 2026-07-26; save select BUILT (untested - no kingdom save on the dev install).
The kingdoms scene loads additively over the title menu (the mode stays MAIN_MENU, nothing
lands on the screen stack); this screen outranks `MainMenuScreen`, which stands down while the
scene owns the menu. Three phases rebuilt in place:

- **Entry menu:** Continue Kingdom (where a save exists), New Kingdom, Back, the game-type
  description.
- **Save select:** each save reads its name plus the widget's own day/difficulty/map labels;
  Enter loads through the widget's click path, Shift+Enter opens the game's delete
  confirmation.
- **Creation wizard**, steps read one at a time, landing on the new step's first element after
  the game's cross-fade:
  - The name field: Enter starts the game's own edit flow - every key then goes to the field,
    typed characters echo, deletions speak "x deleted", Enter accepts and re-reads the field,
    Escape cancels.
  - Gang cards: single-select, "selected" re-announce; the pick also queues the game's own
    narration. Then the gang disclaimer text.
  - Map toggles, each map's blurb tooltip in the buffer.
  - Difficulty presets, named from their definition id (the last slot holds the game's Custom
    copy and takes the game's "Custom" caption), with the preset stat rows as readouts ("Day
    Limit. 60. days", explanation tooltips in the buffer).
  - The wizard's own Continue/Back.
- Escape drives the game's `TryGoBack` end to end: save select closes, each wizard step steps
  back (the name step exits the wizard), the entry menu returns to the title menu (which
  re-announces itself).

> **Note:** While typing, all mod keys pause on the game's own `IsInputtingText` flag
> (device-verified: an injected Down mid-edit moves nothing); the pause also covers the game's
> other rename fields.

**Known gaps:** the save-select phase and the custom-difficulty editing widgets (the row
dropdowns/sliders that appear once Custom is picked) are modeled but not live-verified; the
mods step is swept generically and unverified; the "creating kingdom" hand-off to DRIVING is
unmodeled (the road screens take over after the load).

## 1.6 Patch Notes (`PatchNotesScreen`) - WORKS

Live-verified 2026-07-27; previously the generic floor, which swept only the Close button and
never reached the notes. The overlay (`PatchNotesWidgetBhv`) is a Modal-layer stack entry from
the main menu and PauseModal from pause - both routes verified. Named by its own title label.

- Not a text dump: the page's header (the version heading) is the first row and **each note is
  its own row**, arrowed through like any list.
- **Left/Right flip pages from anywhere on the screen** (the screen's own `HandleAction`)
  through the widget's `TryPreviousPage`/`TryNextPage` - newest page first, so Right walks back
  through history. A flip lands focus on the new page's header (every note below belongs to a
  page that is now gone); at either end the refused flip re-reads the current header.
  Rich-text style tags are stripped by the pipeline.
- Escape/Close close through the screen's own `TryCloseScreen`; the main menu re-announces.

> **Note:** The prefab ships placeholder page text and the widget writes the real page during
> the screen's own open step, so the page reads as nothing until `ScreenState == Open` and the
> settle asks for one re-announce - the entry reads "Patch Notes" then the real version only.
> The same gate covers a reopen, which would otherwise read the page left from last time
> (verified: paged deep, closed, reopened, read page 1).

**Known gaps:** the caption-less prev/next arrow buttons are deliberately out of the tree
(paging is the screen's); no way to jump to a specific version; Home/End move within the
focused list rather than the whole screen, so End from the notes reaches Close but Home from
them lands on the first note, not the header.

## 1.7 Profile Select (`ProfileSelectScreen`) - WORKS

Live-verified 2026-08-01. The panel under the title menu's profile button (`ProfileSelectBhv`),
named by the game's own "Select Profile" title. Two phases:

- The profile list: one flow per slot - the profile's name (the active one leads with
  "selected"; the read-back after Enter is the selection feedback), its Rename Profile /
  Delete Profile buttons (labels from their game tooltips), an empty slot as the game's
  "Create New" - then
  the panel's Back button. Shift+Enter on a profile also opens the game's delete confirmation
  (the confirmation screen takes over). Elements are keyed to the profile guid, so focus
  survives the pooled row swap the game's every refresh performs (rename commits included).
- The create window (Enter on an empty slot): title, name field, language dropdown, the GDPR
  text around the analytics toggle, Continue/Cancel. The game auto-activates the name edit on
  open, so entry reads the title then "editing, enter when done".
- Name edits (create and rename) echo keystrokes and speak the accepted name when the edit
  ends. The rename edit is invisible to the game's own `IsInputtingText`, so the input
  manager asks the screen directly (`EditingName`) to pause the mod's keys for it.
- Escape drives the game's own close (cancels a pending creation, drops the whole panel); the
  title menu re-announces. Deleting the last profile is the game's own silent-refusal path
  (an invalid-click sound, no text - parity).

**Known gaps:** the level diamond (`ProfileLevelBtn`, absent on a fresh profile) is swept
generically but unverified; the save-import flow (`ProfileSummaryWidgetBhv`, console-only
surfaces) is unmodeled.

## 1.7b First-Boot Profile Window (`FirstProfileScreen`) - WORKS

Live-verified 2026-08-03 on a fresh save (SaveFiles removed). With no profile on disk the
game auto-creates a default one and holds the title menu behind its GDPR panel
(`MainMenuUiScreenBhv.m_firstTimeProfileCreationBhv`) with every menu button disabled - the
menu reader used to capture instead and Enter went nowhere. The window is the same
`ProfileCreationWidgetBhv` as the profile-select create window, built by the shared
`ProfileCreationTree`: name field (edit echo + accepted-name read-back), language dropdown,
the GDPR text around the analytics toggle (Enter flips consent, the game's own
`ANALYTICS_ENABLED` option), then Continue, which hands the menu over to the normal
disclaimer flow. The game offers no cancel here, so Escape reports unavailable. The prompt
appears only while no profile exists - a restart auto-saves the default profile and it
never returns, which used to strand first-time users past an unanswerable consent.

## 1.8 Mods Manager (`ModPanelScreen`) - WORKS

Live-verified 2026-08-02 (menu flow) and 2026-08-09 (the panel, with a Workshop mod
installed). The Mods toggle flips the menu to its mods side (a camera timeline; the toggle
relabels to "Return to Base Game", profiles switch to the separate mods set, Import Save
Data appears).

- The mods side's own Confessions and Kingdoms entries live OUTSIDE the menu's serialized
  selectable list and were keyboard-unreachable; the sweep now collects them from their
  containers, so the whole mods flow is navigable.
- Escape follows the game's own `TryGoBack`: it closes the Confessions and mod-confessions
  submenus (whose back arrow is icon-only and invisible to the sweep) before falling through
  to open settings from the top level - this also fixed the base-game Confessions submenu,
  which previously could not be closed by keyboard.
- Mod Confessions auto-opens the mods panel and swaps the menu behind it to mod Continue /
  New Confession plus the "Mods" button that reopens the panel; Escape closes the panel
  (which saves the load order), then the submenu - each step announced.

The panel itself is a dedicated screen (`ModPanelScreen` over the `ModScreenWidgetBhv`
stack entry - every control's caption sits on a sibling object, so the generic floor could
only see Browse Mods). Top to bottom: the game's own "Showing N Mods" count; Enable All and
Disable All as toggles; one row per installed mod - enabled state first, name, version
("off, Deadku's Bounty Hunter Rework + Confessions, toggle, 1.00"), the short and expanded
descriptions and any validation error in the buffer, Enter flips the mod's own enable
toggle, Space grab-and-place reorders the load order through the game's own reorder submit
(grab and cancel speak; a drop reads the resulting order back) - then Browse Mods. Rows
rebuild on an instance-id signature (Workshop sync adds rows late; reorders re-sort), with
elements reused per row so focus survives. Escape = the panel's own close button, which
persists the list. The panel filters by the run type being entered (Confessions shows
expedition-capable mods, Kingdoms shows kingdoms-capable ones; the lists save separately),
so a kingdoms-only mod is absent from the Confessions panel by the game's own design.
Player-verified 2026-08-09: the reorder drop (two mods, Kingdoms panel). Unverified: the
Browse Mods target (the Workshop overlay, external to the game's UI).

## 1.9 Journal - RESOLVED (it is the profile button)

The bottom-right "journal" is the profile select button (`m_profileSelectButton`, prefab
name `Journalbutton`; its onClick is `OnProfileNameButtonPressed`) - the game draws the
profile as a leather journal. Its target surface is the profile select panel, covered under
1.7; the button's read is covered under 1.1.

## 1.10 Store Promos and Mailing List - RESOLVED (in-game half covered; targets external)

Live-verified 2026-08-02. The Origin Pack / Supporter Pack promo buttons (each shown only
while its DLC is unowned) open the game's own external-link confirmation dialog, which the
dialog reader already covers ("External Link. Open external link to view DLC?" with
Continue/Cancel); confirming opens the platform store page - the Steam overlay, outside any
mod's reach. The Mailing List button opens the system browser directly
(`Application.OpenURL`), where the player's screen reader takes over. Nothing further to
model. The same store dialog appears from Kingdoms gang cards and altar DLC rows (see 3.5,
3.8).

---

# Phase 2: System Surfaces (reachable nearly everywhere)

## 2.1 Settings (`OptionsScreen`) - WORKS

Live-verified 2026-07-23; dropdowns 2026-07-28; mod tab 2026-07-27. The game's options screen
(`OptionsMenuUiBhv`), reachable from the title and pause.

- Tab selector + active tab's rows in one vertical flow. Rows: `OptionsItemBhv`
  toggles/sliders (labels/tooltips from loc keys), bespoke widgets (language dropdown
  verified) generically. Toggle round-trip and value re-announce verified.
- **Dropdowns** open an option popup on Enter (verified on the graphics tab, logical and
  device-level key paths): the dropdown label then the first option (the current choice is
  never marked or sought), Up/Down move with no wrap, Enter commits (the game's own
  onValueChanged) and Escape cancels - both close by re-reading the restored dropdown row, so
  a commit reads back its new value. The game's own list is shown/hidden alongside
  (`TMP_Dropdown.Show`/`Hide`), and a screen change tears an open popup down.
- A row holding a second control beside the one its title names reads that control's own
  caption: the resolution row's Update button reads "Update" (live-verified 2026-08-01), not
  as a second "Resolution" item.
- The graphics tab's gamma reset button is icon-only in the game (no text or tooltip), the
  one swept control with a mod-authored label: "Reset Gamma Correction" (live-verified
  2026-08-01). Unavailable while gamma is at defaults, matching the game's interactable
  state.
- A slider's drag handle (a Button in the game's prefabs) is slider plumbing, filtered from
  the sweep like scrollbars; the bespoke graphics quality slider no longer reads a second
  "Graphics Quality, button" item (live-verified 2026-08-01). Left/Right on the slider row
  still adjust the value.
- The audio tab's active audio device row - a static title plus the data-bound device name,
  no control anywhere under it - reads as a read-only readout ("Audio Device, Speakers
  (Realtek(R) Audio)", live-verified 2026-08-04). The rule is generic: a layout row with no
  selectable but a `UiDisplayTextBhv`-bound value becomes a readout; rows with only static
  text (the section dividers) stay decoration.
- The tab is not remembered across close/reopen - the game itself resets to its first tab on
  every open and the mod follows it (corrective re-announce when the game's open animation
  settles the tab after our entry read). Returning from the bindings panel keeps the controls
  tab, because the screen never closed.
- Escape closes in one press from both the title menu and pause (the game's own Escape is
  two-stage on mouse+keyboard; we fold it).

### 2.1.1 Mod Settings Tab - WORKS

The first of the mod's own tabs appended after the game's (live-verified 2026-07-27; each tab
is a `ModTab` under `Screens/Options/`, the screen handling them generically): mod-authored
rows instead of swept widgets - the announcement separator, a free-text field
(`TextEntryElement` + `ModTextEdit`), the sensing range, a numeric text field
("sensing range, edit, 80") typed to any value and clamped to 20-200 on commit (garbage
commits nothing, empty restores the default, spoken "reset to default"), and the
auto-collect toggle ("auto collect pickups, toggle, off", default off - the road layer's
hands-free pickup mode, 5.2). The road layer reads the sensing range live for the pickup
pings (live-verified 2026-08-02: raising it on the road started ten real pickup loops,
restoring 80 drained them).

- Enter opens the mod's own typing mode - "editing, enter when done" then the bare-Enter
  outcome spoken as a hint (the suggested value is spoken rather than prefilled, so typing
  starts clean) - with layout-aware characters echoed, Backspace erasing, and Escape
  cancelling with a full row re-read. Enter commits and the new separator applies to every
  announcement immediately; committing nothing resets to the default (", ").
- The session dies silently with its owner (closing the screen under an open edit never wedges
  the keys - live-verified). The IME is enabled per session so CJK composition can type into
  the mod-drawn field (composition owns Enter/Escape; not yet live-verified with a CJK IME).
- Persisted through BepInEx config (values quote-wrapped so a separator's edge spaces survive
  the config parser's trim), verified across a game restart. The value reads spelled character
  by character (a separator is punctuation, inaudible whole).
- While the mod tab is up the game's own tab state is untouched; a mouse click on a game tab
  leaves it.

**Known gaps:** DEBUG-tab filter field unhandled; sliders speak normalized percent, not the
game's display value; the mod tabs are invisible to sighted users (no game-side tab button is
injected).

### 2.1.2 Key Bindings (`KeyBindingsScreen`) - WORKS

Live-verified 2026-08-01. The controls tab's Bindings button opens the game's key-bindings
panel (`InputBindingsWidgetBhv`), which takes over as its own screen named by that button's
caption.

- One row per rebindable command, labeled with the command name from the row's data context:
  Up/Down walk commands ("Inventory, Key 1, button, I"), Left/Right its two key slots, with
  the game's action-map headers (General, Driving, Combat) in the flow; the column-header row
  is skipped (the slot labels carry Key 1/Key 2). Close and Default Bindings close the list.
- Enter starts the game's interactive rebind; its "Setting Key" prompt reads back as the
  activation feedback. While the listen is up EVERY mod key pauses (`RebindActive` in the
  input manager's suppression), so the next key pressed becomes the binding - arrows, Enter,
  Tab included. The end of the listen reads the slot's outcome: the new key, or the kept one
  after Escape (which the game's own rebind consumes; the panel stays open).
- Shift+Enter clears the slot ("Not Set" read back); the game's duplicate-removal on a
  completed rebind clears colliding slots silently, read when visited (parity - the game
  shows the same without fanfare).
- Header and key labels read from the rows' data contexts, not their TMP labels - the pooled
  header still shows its placeholder on the entry announcement.
- Escape closes the panel through the game's own toggle; the settings screen re-announces.

### 2.1.3 Mod Sounds Tab (`ModSoundsTab`) - WORKS

Live-verified 2026-08-02 (logical and device-level key paths, remembered-tab reopen across a
game restart); master volume row 2026-08-04; group tabs 2026-08-04. The mod's sounds
glossary, the second mod tab: the master volume slider, then a group tab per sound family -
road, combat, the assets/audio folders, derived from the cue names (`AudioCues.
GroupOf`) so a new cue lands in its tab by name alone - then one row per `AudioCue` in the
active group naming what the sound is used for ("pickup nearby, 100 percent"). Left/Right on
the group tab switch groups (rebuilding the rows below; a running preview stops); the group
resets to road on each open, matching the settings screen's own no-remembered-tab behavior.

- The master volume row heads the tab ("master volume, slider, 100 percent"): Left/Right
  step the baseline every mod sound plays at. Per-sound volumes are stored as signed offsets
  from it (`Master = 80`, `RoadTurning = -20` in the config's `[Sounds]` section), so a
  master move carries every sound while their relative levels hold; the rows still display
  the resolved absolute percent, never the offset (live-verified 2026-08-04, on the
  since-removed collection cue: it stepped to 60 under master 80 and read "80 percent"
  again after master returned to 100 while pickup nearby read "100 percent"). An effective
  volume clamps at 0 - never negative - and
  the clamped offset survives a master dip below it intact. Pre-master configs (bare unsigned
  numbers) read as absolute percents against a full master, unchanged until next adjusted.
- Enter plays the row's sound once; Space (the grab key) toggles it looping. Both are
  SILENT - the sound itself is the feedback ("playing" leads a looping row's line, e.g. in
  the buffer). The loop stops on moving focus off the row, switching tabs, and closing the
  screen. Previews play centered at the sound's saved volume.
- Left/Right step the row's volume 0-200 in tens (master row included; live-verified
  2026-08-04 to the 200 cap), speaking the percent (of the sound's natural level, values
  above 100 boosting past it under the output's soft limiter; "minimum"/"maximum" at the
  ends); a running loop re-aims live on each step,
  so the change is heard as it is spoken. Values
  persist per sound in the config's `[Sounds]` section (verified across a restart) and scale
  every playback of that cue - one-shots and loops - through the volume-scaled engine, with
  the natural dynamics (distance attenuation, pan) still the caller's.
- The game's late tab-index moves after the screen reports Open do not read as player clicks
  (only a change interrupting a settled index is), so the mod tab stands through the open
  sequence.

### 2.1.4 Mod Keys Tab (`ModKeysTab`) - WORKS

Live-verified 2026-08-02 (physical keys and device-level injection, chords included, plus the
driving handover below; the add/delete menu re-verified same day). Rebinding for the mod's own
commands, say-the-spire2's model: one row per registered input action in registration order,
each carrying a LIST of keys ("Activate control, Enter, NumpadEnter").

- Enter opens the row's menu as a popup: "add key" plus "replace {key}" and "delete {key}"
  choices per current key (replace swaps the listened key in for that one, same refusal
  rules); Escape backs out with the row re-read. Choosing "add key" listens ("press the new
  key" - delivered as the restored row's own focus text, so the popup close cannot talk over
  it): the next non-modifier key pressed joins the set, with whatever Ctrl/Shift/Alt are held
  at that moment, so chords like Ctrl+PageUp capture naturally. Escape keeps things as they
  are. While listening every mod key pauses (the same suppression as text entry and the
  game's own rebind listen).
- A captured key another command holds is REFUSED and named ("DownArrow, already bound to
  Navigate down") - delete it off that command first; no command is ever stripped behind the
  player's back. A key the command already carries reads the row back unchanged.
- Shift+Enter (the discard chord) restores a row's authored defaults, spoken with the
  restored keys. A row with every key deleted reads "not set" and stays that way across
  restarts (the stored "none" sentinel).
- Bindings persist per command in the config's `[Keys]` section ("F1", "F1|shift",
  "Enter;NumpadEnter"); empty means the defaults stand, and an entry that does not parse is
  dropped with a log warning so a stale config never bricks a key.
- The shared-keyboard screens' game-key suppression follows the LIVE bindings: rebinding both
  panel keys off Tab hands Tab back to the game (its minimap opened on Tab on the road,
  verified), the new key's game binding rests instead, and a reset re-claims Tab the same
  frame - the road map's arrow/bare-Ctrl claim derives the same way, pad paths included.
- CONTROLLER (verified 2026-08-02 with a synthetic Input System gamepad; no real hardware
  pass yet): the game supports pads (InputDeviceHandlingType, the Device Handling dropdown),
  but the input gate disables its action maps under a captured screen, so the game's own pad
  navigation is dead there - the mod's pad defaults are what makes captured screens
  controller-usable: dpad navigates, A activates, B backs out, shoulders cross panels, the
  right stick reviews buffers (say-the-spire2's layout). Rows list pad combos beside keys
  ("Navigate down, DownArrow, DpadDown"); "add button" appears in the row menu while a pad
  is connected and captures on RELEASE, so a held trigger becomes the combo's modifier
  ("LeftTrigger+X"); trigger state must match exactly (bare X never fires a LeftTrigger+X
  combo). Mixed lists persist typed ("DownArrow;pad:DpadDown;pad:X|LeftTrigger"). Any pad
  press silences ongoing speech (say-the-spire2's behavior), ahead of the input tick so the
  press's own announcement is not the thing cut.

### 2.1.5 Mod Announcements Tab (`ModAnnouncementsTab`) - WORKS

The mod announcements tab (second in the mod-tab row, after mod settings): one toggle per
optional mod announcement, from `ModSettings.Announcements` (`BoolSetting` rows rendered by
`ToggleSettingElement`, reading like the game's own toggles - "on, corpse deaths, toggle";
Enter flips, persists through BepInEx config, and re-reads the new state). Toggles are read
live at the gating site, so a change applies to the next line spoken. Current toggles:

- **Corpse deaths** (default on): whether a corpse's own destruction speaks its died line in
  battle (smashed by a skill, crumbled on its round timer), judged by the game's own corpse
  test (`AudioConditionUtils.IsCorpse`). The battle-end sweep of leftover corpses and capture
  teardowns stay silent regardless (no death presentation to stand in for).

Live-verified 2026-08-02 (logical path): the tab reads in the row after mod settings, the
toggle flips with the on/off re-announce, the value persists to the config, and the combat
gate reads the live value. The gate's effect on an actual corpse death needs a fight.

## 2.2 Pause Menu (`PauseScreen`) - WORKS

Live-verified 2026-07-23. A widget on a generic `UiScreenBhv` prefab
(`PauseMenuUiControllerBhv`), opened by `CommonUiBhv.TogglePauseMenu`.

- Buttons from the game's own navigation order: Return, Glossary, Options, Tutorials, Patch
  Notes, Feedback, Exit. Decorative selectables with no text source (the profile badge)
  skipped.
- Escape = the menu's own Return. Options-from-pause round trip verified.

## 2.3 Confirmation Dialogs (`ConfirmationScreen`) - WORKS

Live-verified 2026-07-23 with the exit-game dialog (`ConfirmationDialogBhv`).

- Title + body first, then confirm/decline; Escape declines; the underlying screen
  re-announces with focus restored to the button that opened the dialog.

## 2.4 Generic Modal (`UiModalScreen`) - BUILT

`UiModalBhv` (`title_text`/`body_text`). No UiModal appeared during live testing yet.

## 2.5 Token Glossary (`TokenGlossaryScreen`) - BUILT

Deployed, needs live pass. Previously the generic floor: a flat list of "button"-role rows
named after the legend's first label, with no category info. The overlay
(`TokenGlossaryWidgetBhv`) is a stack entry openable over pause, combat, the inn, the academic
view; named by the pause menu's own "Token Glossary" caption.

- The game shows a flat token list whose name colours encode the category, decoded by an
  on-screen legend of colour pips. The mod reads each category as **one labeled horizontal
  row** (the legend's own caption strings; Left/Right within a row, Up/Down between
  categories) holding its tokens in the game's order.
- The colour-to-caption pairing uses the game's own data both ways: each row's bound
  `name_colour` is the exact hex the game reads from its `glossary_*` colour loc strings,
  matched back to the legend caption keys (`buffs_label`, `glossary_stealth_label`,
  `debuffs_label`, `glossary_other_label`, `glossary_special_label`,
  `glossary_enemy_type_label`). Hero tokens carry special's exact colour and biome tokens
  unique's, so they fold into "Hero & Combo" / "Unique" just as they do visually; a colour
  matching no group reads uncategorized, the same unexplained shade a sighted player sees.
- Rows are plain entries, not buttons: the game wires no click or submit to them (their
  Selectable only anchors the controller highlight). The token's name is the focus line; the
  full description is the buffer. Escape closes through the game's own `HideTokenGlossary`.
- The list's contents are the game's own context filter (viewed tokens, combat-contextual
  tokens, the party's hero tokens, the kingdom gang's uniques, biome tokens while driving/in
  combat), so the same screen legitimately lists different tokens per surface.

## 2.6 Tutorial Archive (`TutorialArchiveScreen`) - BUILT

Deployed, needs live pass. Previously the generic floor: bare title buttons, no text on
activation, no unviewed marker. The pause menu's Tutorials screen
(`TutorialArchiveWidgetBhv`, also pushed by the game with a specific tutorial), named by its
own "Archive" title (`tutorial_menu_title`).

- One element per entry in the game's own order (majors, then their category's minors); each
  reads its title (`tutorial_t{eventId}_title`) **prefixed "New"** (authored, translatable)
  while the game shows the entry's unviewed notification icon (the row's bound
  `notification_icon`, live - the prefix drops the moment an entry is viewed).
- **Enter is the game's own option click** (`OnOptionClicked`): the game opens the entry's
  text in the side panel, marks it viewed, and saves; the mod speaks the landed title and full
  description back (composed from the same `tutorial_t{eventId}_*` loc keys, not the
  frame-late panel TMPs), and the focused entry's buffer carries the description line by line
  while it is the one on display. Escape closes through the screen's own teardown.

> **Note:** These options **activate on uGUI selection** (`OnSelect` drives
> `ViewTutorialType` - the game's controller path), so browsing must never mirror our focus
> into the EventSystem: scrolling would view-and-clear every entry passed over. Our elements
> read without selecting; only Enter commits.

Not modeled: the tutorial's image/video (visual-only), the close button (Escape covers it).

## 2.7 Feedback (`FeedbackScreen`) - WORKS

Live-verified 2026-08-02 on the road (pause menu > Feedback). The game's user-report form
(`UserReportingUiBhv`, Unity's user-reporting package in LEGACY uGUI widgets - `InputField`,
`Dropdown` - which the generic paths misread). The privacy confirmation dialog leads (an
ordinary dialog read); Send Report captures a screenshot - the screen enters on its capture
phase reading the game's progress text as its name, and the form's buttons arrive late,
rebuilt in by instance-id signature.

- Summary and Description read as edit rows titled by their game placeholders ("Summary
  (Required field), edit") with the typed text as the value. Enter starts the field's OWN
  edit: typing flows in at device level while every mod key pauses (the package never sets
  the game's IsInputtingText - the screen's isFocused scan feeds the input manager's
  suppression), keystrokes echo from the field's text diff, and the edit's end reads the
  field back. Verified live: suppression (device arrows move nothing mid-edit), per-char
  echo and end-of-edit read-back via simulated text changes; physical typing rides the same
  path as the game's own fields (injection cannot synthesize text events).
- The category is a legacy Dropdown ("Bug, dropdown"), opened as an option popup like the
  TMP ones (options walk - "Performance Issue" - Enter commits via the game's own
  onValueChanged, Escape restores; verified live).
- Cancel / Submit / Take Screenshot sweep generically; Submit sits "unavailable" until the
  summary validates and reads available the moment it does (verified). Escape is the form's
  own cancel (`CancelUserReport`), the pause menu re-announcing behind it.
- Observation: the privacy dialog sometimes continues on its own moments after appearing
  (seen with and without window focus; it also waited indefinitely once) - likely persisted
  consent or the dialog's own hotkey handling. Harmless either way: both states read
  correctly.

**Known gaps:** submitting a real report end to end has deliberately never been exercised;
the description field may be multiline (Enter might insert a newline rather than end the
edit - Escape always ends it).

## 2.8 Credits (`CreditsScreenWidgetBhv`) - NOT STARTED

Unaudited; no dedicated reader.

## 2.9 Generic Floor (`GenericScreen`) - WORKS

Live-verified 2026-07-23, originally on the hero sheet before its dedicated screen existed.

- Any pushed SCREEN stack entry with no dedicated reader gets a generic sweep of its labeled
  selectables, so no pushed surface is dead air. Registered above the mode screens (a pushed
  screen covers the scene) and below the dedicated stack screens. Driving HUD widgets
  (minimap, goals - non-SCREEN stack entries) are excluded so free driving is never captured.
- Escape closes a `SubScreenElementBhv` panel through its own `CloseSubscreen`, a raw
  `TryCloseScreen` otherwise: a hub re-enables its own controls only in the panel's close
  flow - a raw pop of the altar's stagecoach-tracks panel left every altar region marker
  disabled (observed live 2026-07-24; the in-place repair is the game's own
  `CheckToEnableSubScreenButtons`).
- Results surfaces read fully on this floor - see 9.1.

---

# Phase 3: The Altar of Hope (ALTAR_OF_HOPE mode)

Visited between confessions to spend Candles of Hope. Previously dead air - the altar is a
mode surface with no stack entry.

## 3.1 Altar Hub (`AltarScreen`) - WORKS

Live-verified 2026-07-24 on the first-visit intro altar, including player-driven candle
spends.

- The candle balance ("Candle of Hope, 5" - the game's own item name over the profile's live
  CANDLES value), then the six region markers of the altar map as one list (named by the
  game's `altar_region_<key>_name` strings). A region the game has disabled reads
  "unavailable" - the game locks by disabling the Selectable COMPONENT, which a generic sweep
  misses - with the sub-screen's own unlock requirement in the buffer (the text the sighted
  lock tooltip shows: "Unlock all heroes to gain access to The Mountain").
- Then **The Recollection** (the collection gallery has NO region marker - the sighted path is
  the panel tab bar, so the hub lists it after the regions, opening through the collection's
  own `ToggleSubScreenElement`; hidden on the intro altar like its bar button), then Embark.
- Enter on a region is the game's own submit (opens its sub-screen); Embark drives `OnEmbark`
  with its spend-your-candles-first reminder dialog; Escape opens the pause menu.

**Known gaps:** the hub's milestone pool readouts (candle-threshold rewards) were empty on the
intro altar and are unread; Embark's press is verified only up to (not including) the exit.

## 3.2 The Working Fields - Item Unlocks (`AltarItemScreen`, `AltarRevealScreen`) - WORKS

Over `AltarItemSubScreenBhv`. (Renamed from AltarRecollectionScreen: the game's "The
Recollection" is the gallery in 3.8; this panel's progress line merely says
"Recollection: 9/163".)

- Reads: balance, the total line ("Recollection: 3/163"), and the unlock-category buttons with
  progress and cost composed from their bindings ("Trinkets, 1/73, 1 candle" - authored plural
  for the cost the game shows as icon+number).
- Enter purchases in ONE press by driving the game's own (private) `Purchase` - the game's
  gesture is a mouse hold, and a synthetic hold risks re-purchasing if the reveal timeline
  ever skips its pause; the purchase self-validates, so a no-op answers "unavailable".
- Escape closes through the panel's own `CloseSubscreen` - a raw `TryCloseScreen` skips the
  altar's pop flow and leaves every region marker disabled (found live; the repair is the
  game's own `CheckToEnableSubScreenButtons`).
- **The item reveal reads as a modal** (`AltarRevealScreen`, outranking the panel): while a
  purchase presents, the one element speaks "unlocked" then the item's name and full
  description (buffer-reviewable line by line); arrows cannot wander mid-reveal, and Enter or
  Escape continues (the game's own Submit step). The screen matches only once the name binding
  holds THIS reward's name (`item_name_<activeRewardId>`), because the binding lags the
  purchase by an icon load - without the gate the previous reveal re-reads on the next
  purchase (observed live). On return the panel re-announces with focus restored onto the
  purchased category, so its updated count is the landing line and another Enter pulls again.

**Known gaps:** the reroll variant of the panel (`m_isRerollScreen`, after full completion)
shares the class and should read identically but is decades of candles away.

## 3.3 The Dam - Game Options (`AltarOptionsScreen`) - WORKS

Over `AltarOptionsSubscreenBhv`, live-verified 2026-07-28.

- One settings row per altar option, reusing the options screen's own row element - the
  generic floor read this panel as dead air because each Toggle is a bare checkmark object
  with its caption in a sibling label.
- A row the profile has not earned reads its state plus "unavailable" and carries the game's
  own unlock-requirement line in the buffer (the game swaps it into the row's tooltip binding
  on `SetLocked`); an earned row toggles with Enter and reads back its new state. The profile
  saves through the panel's own close (Escape).

> **Note:** The option rows spawn a beat after the stack entry appears, so the screen follows
> an instance-id signature and re-announces when the first fill lands late - a one-shot build
> entered on the entry frame stayed empty forever (observed live 2026-07-28).

## 3.4 The Living City - Hero Tracks (`AltarClassScreen`) - WORKS

Over `AltarClassSubScreenBhv`, live-verified 2026-07-28; purchases unexercised.

- The candle balance, then one horizontal row per roster hero: the hero's icon button first
  (name + the track's spent/total binding), then the track's milestone diamonds left to right,
  each named by its reward tooltip's headline with the candles still needed ("New Skill,
  button, 8 candles") or "unlocked" once bought, the reward description in the buffer.
- Up/Down move between heroes announcing the hero as row context (the row label dedupes
  against the icon element on the row head); rows remember their column, and Up from the top
  row climbs out to the candle balance (the navigator's vertical spill previously stopped at
  the first enclosing vertical list, stranding the balance above the row block - user-caught).
- Enter on a diamond drives `AttemptToPurchaseMilestone` (buy-up-to-this-milestone, the hold's
  meaning) behind the same full-affordability check the game gates the hold on, answering
  "unavailable" when short or bought. The generic floor previously read this panel as a flat
  list of identical-label diamonds with a dead Enter: `ProgressTrackMilestoneBhv` has no
  submit handler - the game sells only through a pointer/Submit-action HOLD gated on the
  diamond being the EventSystem selection.
- Enter on the hero icon is the game's own `OnIconClick` - one candle banked into the track
  (or the store dialog on an unowned-DLC row), reading back the moved total. A quest-locked
  hero (disabled icon button, no diamonds spawned, tooltips suppressed) reads "unavailable"
  with the game's lock caption in the buffer.
- The panel spawns rows one per frame on open, so the tree follows an instance-id signature,
  reusing built rows to keep focus. Escape closes through `CloseSubscreen` (the same
  region-marker trap as 3.2). Purchase paths are code-verified only.

## 3.5 The Intrepid Coast - Upgrade Tracks (`AltarGeneralScreen`) - WORKS

Over `AltarGeneralSubScreenBhv`, live-verified 2026-07-28; spends unexercised.

- The candle balance, then one horizontal row per stat track (Journey, Resourcefulness,
  Companionship, Renown, The Infernal Flame): the track's icon button first (name +
  spent/total; the game binds the loc KEY `altar_upgrade_<id>` as the raw context value, so
  the mod localizes it), then the milestone diamonds with reward names, costs, and buffer
  descriptions, all through the shared `AltarTrackElement`/`AltarMilestoneElement`.
- Enter on the icon banks one candle (`OnTrackSpendAttempt`), Enter on a diamond buys up to
  it. Escape closes through `CloseSubscreen` (region markers re-enable, verified).

## 3.6 The Timeless Wood - Memories (`AltarMemoryScreen`) - WORKS

Over `AltarMemorySubScreenBhv`, live-verified 2026-07-28 - hero rows via the game's own
all-memories dev pref, since the dev profile has no run-survived hero.

- The candle balance, the Memory unlock track as a shared track row (icon + milestones - the
  track's milestones spawn a beat after the stack entry, so the rebuild signature includes
  them), the game's "Heroes with memories are required" notice when it shows, then one
  horizontal row per memoried hero.
- Each memory slot is keyed to a confession boss and named by the game's own
  `boss_choice_<id>_label` ("I. Denial") - the identity the sighted slot carries as a boss
  sprite - reading "empty" (Enter opens the game's selection list), "unavailable" (Enter shows
  the game's run-locked dialog), or the held memory's item name with its tooltip in the buffer
  and, once earned, "reroll N candles" on Enter (the game's own paid reroll).
- The open selection list swaps the tree modal-style: one element per offer (item name +
  candle cost, tooltip in the buffer), Enter committing through the game's own select-and-buy
  pre-gated on the cost (the game's own failure path would close the whole list), Escape
  closing back to the opened slot, whose re-announce is the dismissal read; mid-reroll Escape
  answers "unavailable" because a paid reroll must pick an offer.

**Known gaps:** an actual memory purchase, the reroll flow, and a natural locked slot are
unexercised live (all need a run-survived hero on the profile).

## 3.7 The Mountain - Cosmetics (`AltarCosmeticScreen`, `AltarCosmeticRevealScreen`) - PARTIAL

Over `AltarCosmeticSubScreenBhv`; browse side live-verified 2026-07-28 via the game's own
unlock-cosmetic-altar dev pref (the profile has it legitimately locked).

- The candle balance, then one reward button per hero ("Man-at-Arms, 0/6, 3 candles") named by
  the class string of the button's unlock-track id (the sighted button is a bare portrait),
  through the shared `AltarUnlockButtonElement` (generalized over presenting/resume delegates
  instead of the item panel type).
- A DLC-locked hero button reads "unavailable" with the game's caption in the buffer and Enter
  raises the game's own store dialog (the game's release handler).
- The reveal screen mirrors the recollection's but needs no name-match gate: the cosmetic
  panel writes item_name/item_desc synchronously when a purchase lands, so presenting plus a
  non-empty description is current by construction.

**Known gaps:** purchases, the reveal, and the DLC-locked read are unexercised live.

## 3.8 The Recollection - Collection Gallery (`AltarCollectionScreen`) - WORKS

Over `AltarCollectionSubscreenBhv`, live-verified 2026-07-28; always unlocked.

- A filter tab selector first (All Items, Combat Items, Trinkets, ... - Left/Right switch via
  the panel's own `OnInventoryFilterPressed`, rebuilding the list), then every collected item
  as a browse-only row: title, "New" while the game's unviewed marker shows (the game clears
  it for the next visit as it lists the item), full item tooltip in the buffer.
- The game lists items one per frame, so the tree follows the live set by instance-id
  signature, reusing elements so focus holds while rows fill in below.

---

# Phase 4: The Crossroads and Embark (HERO_SELECT, EMBARK modes)

## 4.1 Crossroads (`CrossroadsScreen`) - WORKS

Live-verified 2026-07-23. The pre-run hub (the HERO_SELECT mode - `HeroSelectBhv`,
`EmbarkUiBhv`, hero widgets `HeroSelectActorUIBhv`).

- Party ranks (the game's "roster slots", Rank1-4), then the hero pool as horizontal strips,
  then the actions strip: the party's name when the composition has one, the shown hero's
  name controls, the two overlay openers, **Embark** (appears once all four ranks are filled -
  drives the game's own `ConfirmRosterSelection`, including its unequipped-skills confirmation
  dialog), and **Random Party**.
- Each party slot LEADS WITH ITS RANK ("rank 1, Highwayman" / "rank 1, empty slot";
  live-verified 2026-08-02) - rank 1 is the front line, the same numbering combat uses, and
  it is what tells the four otherwise identical empty slots apart. The slots run rank 4 to
  1 left to right (reordered 2026-08-08), the same direction the combat battlefield row
  walks the party. Pool heroes keep their bare name, and a grab announces the hero, not
  the rank.
- **Landing on a hero shows them** (the game's own `OnActorSelected`, silent - `playAudio`
  false): the canvas model, the stat block, and the targets of the name, reroll, and path
  controls all follow our focus. Display-only; it never touches the party. Without it those
  controls acted on whichever hero the game happened to display on entry, which no keyboard
  move could change. Empty ranks and locked heroes hold the previous hero (no actor to show).
- **R renames the focused hero, Shift+R rolls them a new name** (live-verified 2026-08-02,
  from a party rank and from the roster). Hero-targeted like the inspect key, mirroring the
  game's own controller model, where rename is a hotkey on the shown hero rather than a field
  to navigate to - the game puts both on one key by tap-versus-hold, which a hold makes poor
  for a screen-reader user, so each gets a key. The rename runs the game's edit flow
  (keystrokes echo, the accepted name reads back, the mod's keys pause meanwhile); the reroll
  speaks the new name, which the game otherwise changes silently. A non-hero element answers
  "unavailable" rather than going quiet. The pair lives in the Roster input category, declared
  only by this screen - everywhere else R is free (in combat it is the hero-4 glance).
- "reset hero" (a run survivor's cosmetics/memories restore, the game asking to confirm)
  stays in the actions block; it is icon-only in the game, so it takes an authored label.
- Hero labels are the game's own class-name loc keys; locked heroes say "unavailable" with
  their flavor/unlock text as buffer lines; drafted pool heroes read "in party". Every hero
  slot's buffer ends with the class blurb the sighted panel shows
  (`actor_verbose_description_*` / `actor_descriptors_*`: the flavor line and the "+ Front
  Rank + Guard..." descriptor list); the same lines lead the hero sheet header's buffer.
- **Enter and Space are the same move** (unified 2026-08-02, live-verified both keys through
  a full pool -> rank -> pool cycle): pick the focused hero up, then place them on the next
  slot you press on, through the game's own drop logic (specific rank, rank swap, back to the
  pool), with grabbed / cancelled / cannot-place feedback and the landing slot read live. On
  a slot with nothing to pick up (an empty rank, a locked hero) it answers "unavailable"
  rather than eating the press.
  The game's own Enter two-step is deliberately NOT used: it armed hidden selection state
  (`SelectingRosterSlot`, `SelectedHero`) and moved the game's own cursor, which desynced from
  our focus - a stale armed state was caught live, and while it stood the game suppressed the
  hero-display switch. All grab state is now mod-side and commits in one call, so nothing is
  left armed (verified: `selectingRosterSlot` stays false across an Enter-driven move).
- **C** = the hero sheet (the mouse right-click equivalent, matching the game's own "Hero
  Sheet (C)" hint); Escape closes it.

**Known gaps:** the Embark element is live-verified up to (not including) the press - pressing
it starts the run. The party's aggregate Rank/Target pips are not read (each skill's exact
ranks are in the hero sheet).

## 4.2 Hero Sheet (`CharacterSheetScreen`) - WORKS

Live-verified 2026-07-23 from the crossroads; relationships tab 2026-07-24. First met here;
the same screen serves the inn, the road, and combat (opened with C / Enter on a hero).

- Layout: hero header (name, then class and path; **Left/Right page through the heroes**, the
  path description is buffer lines), the sheet's tab selector, then the active tab's content.

### 4.2.1 Skills Tab (the sheet's main view) - WORKS

Reads from the game model:

- HP/stress/speed, each with its tooltip breakdown as buffer lines.
- The nine resistances (displayed value; base/modifier breakdown in the buffer).
- Quirks (name; description in the buffer, re-read live so rerolls never go stale).
- Each combat skill as a toggle - Enter equips/unequips through the game's own button - with
  the full skill card as buffer lines (Rank/Target lines with multi-hit "+" joins,
  DMG/CRIT/cooldown, per-target effects, melee/ranged). An unmastered skill's mastery
  preview - the sighted tooltip's hold-to-expand half: the upgraded stat bar and effects -
  is the mastery buffer, one Ctrl+Right away (spliced card lines live-verified 2026-08-03 on
  Backlash, before the preview moved to its own buffer). A mastered skill (the game's button
  carries the `_u` id, so its card already reads the upgraded values) leads its state with
  "mastered", the laurel's spoken form, and its mastery buffer answers "no upgrade
  available"; the combat bar speaks the same "mastered" word.
- The combat item and trinket slots. Resistances, quirks, skills, combat items and trinkets
  are one horizontal row each (Left/Right within a row, Up/Down between sections).
- Equip slots (trinkets, combat items) are `EquipSlotElement`s: occupied slots read the item's
  own title from the model, empty ones their caption, and activation speaks the landed state
  (live-verified 2026-07-24 via the inn equip flow, both directions).

### 4.2.2 Relationships Tab - WORKS

Live-verified 2026-07-24: all rows with values, buffer chain, jump both ways with the hero
announce.

- Each partner row is a dedicated element (`RelationshipRowElement`): the partner's name with
  the affinity readout the sighted banner shows on the focus line - the band word and pip
  meter ("Paracelsus, button, Neutral, 9/20") while affinity builds, or the formed
  relationship's name (plus remaining days in Kingdoms) once one exists - all live from the
  row's own data bindings (`affinity_name` localized, `pip_value`, the game's own Kingdoms
  gate for the duration).
- The full affinity tooltip (band description, formation-chance breakdown with per-quirk
  contributions) is the buffer, line per line.
- Enter is the game's own click - it moves the sheet to that partner - and speaks the
  destination hero's name so the switch is never silent (the landing row announce can read one
  frame stale off the reused ring widgets; the hero name is the reliable signal).
- The unviewed-change notification icon is deliberately unspoken - the game clears it the
  moment the tab opens, so one glance is all sighted players get too.

Unexercised: a formed relationship (all Neutral in the test run; the tooltip mechanism is
shared so buffers carry its description) and the Kingdoms duration line.

### 4.2.3 Other Tabs (Conditions, Story, Cosmetics) - WORKS

Story reads as a generic sweep of the tab panel's labeled selectables, with the panel's own
text - or "empty" - as the floor. Verified live: Relationships "empty" pre-run, Story its
unlock hint.

The Conditions tab (2026-08-12) reads one row per condition - the panel has no selectables,
so the sweep used to fall to the all-text floor and the whole tab smooshed into one line -
with the condition's source (the granting inn, the trophy) as its tooltip in the buffer,
then the hero's run goal under the game's "Hero Goals" title (progress in the game's own
text, the candle/loot reward tooltip in the buffer) and any memories under its "Memories"
title. Empty sections vanish rather than reading as stops; a hero with nothing at all reads
"empty".

The Cosmetics tab (2026-08-12) reads as the game's own three sections - "Hero Palette",
"Weapon Kit", and "Hero Skin" when the hero has skins - one named swatch per cosmetic: the
name is the game's own string for the resource (the swatch itself is a color patch or a
two-letter code; only the tooltip named it before, and the row read as bare numbers),
"selected" marks the applied choice live, a locked skin refuses with "unavailable" (its
unlock hint stays in the buffer), an unviewed cosmetic carries the game's "New" marker, and
Enter applies through the button's own submit. The game offers the tab only at the inn and
the crossroads (`IsTabAvailable`); a tab activating late no longer stays missing from the
selector - tab availability is part of the per-frame signature.

Verified overall: equip toggle round-trip (on/off/on), hero switching rebuilds all content,
tab switching (both our selector and the game keeping the tab across hero switches), Escape
closes through `HideCharacterSheet` with the crossroads re-announcing, physical **I** key
entry from a hero slot.

**Known gaps:** hero rename (the name input field and edit button) is not modeled; the game's
own tab hotkeys and tooltip-view mode are not used.

## 4.3 Crossroads Overlays - MOSTLY COVERED

Canvas overlays on the hero-select scene. They are NOT stack screens, so each matches off the
game's own panel flag and registers above the crossroads; their opener buttons are surfaced
on the crossroads now that the panels read (the trap rule in 4.1 is satisfied).

### 4.3.1 Path Select (`PathSelectScreen`) - WORKS

Live-verified 2026-08-02. The "Change Path" seal opens it (`TogglePathSelectionPanel`), named
by the panel's own title ("Hero Path").

- One row per available path. Enter previews through the game's own `SelectPath`, which only
  drives the comparison panel and arms the confirm button - the path itself changes on
  confirm, so browsing is side-effect-free.
- A "path details" readout whose buffer carries the previewed path's whole card, line per
  line. Since 2026-08-08 the lines come from the shared `PathComparison` reader (the panel's
  own data context plus the coverage pips as "Skills per rank" / "Skills per enemy rank"
  count lines, replacing the bare header words) - the same reader live-verified on the
  mastery trainer's panel; this surface not re-walked since the switch.
- The confirm button commits (`SetSelectedActorPath`), reading "unavailable" while the
  previewed path is already the active one. Escape closes through the game's own toggle.

### 4.3.2 Party Loadouts (`PartyLoadoutScreen`) - WORKS

Live-verified 2026-08-02 (a test loadout was created, read, renamed, and deleted). Opened by
the Party Loadouts button, named by the panel's title.

- One block per saved loadout: its name (verified "Loadout1"), the heroes it holds as buffer
  lines from their portrait tooltips, then the row's rename and delete buttons - icon-only
  game-wide (no text, no tooltip), so they take authored labels.
- Enter on the loadout applies it to the party (the game's `OnClickSubmit`); rename runs the
  row's own edit (keystrokes echo, the accepted name reads back); delete removes it and the
  pooled rows rebuild by instance-id signature.
- Save Loadout stores the current party (needs at least one hero); the panel's Continue
  button and Escape both close.

### 4.3.3 Infernal Flame Vitrine - WORKS (generic floor)

Live-verified 2026-08-02. The run's boss-blessing modifier gallery, opened at the crossroads
by the game's "StageCoach" key (Z) - `HeroSelectBhv.HandleInputVitrine` routes that key to
`CommonUiBhv.ToggleTorchCompletionScreen`, NOT to a stagecoach panel. Read by the generic
floor and complete as-is: the screen names itself, one row per flame ("The Fragile Flame",
"The Doom Candle", ...), each row's buffer carrying the whole modifier card - flavour, then
the mechanics line by line ("+20% Traveling Flame Drain", "Loathing Max: -1", the per-flame-
level hero and enemy effects). Escape returns to the crossroads.

**Known gaps:** the currently active flame is not marked (unverified - the test run has no
blessing set).

**There is NO stagecoach config at the crossroads.** An earlier audit pass listed one; live
probing found no `StageCoachConfigUiBhv` in the hero-select scene and no coach field on
`HeroSelectBhv`. That panel exists only at the inn (the Wainwright) and on the road, both
covered in 8.5.

## 4.4 Embark Staging (`EmbarkScreen`, EMBARK mode) - PARTIAL

Depart-only case live-verified 2026-07-25; relationship rows unexercised - they need a mid-run
embark with new affinities. The scene between the crossroads (or an inn) and the drive: an
intro plays, then the game waits for the depart press - previously dead air (a mode surface
with an empty screen stack), where a sighted player's keys fell through to the game unspoken.
Named "departure".

- One element per pending hero relationship (`EmbarkRelationshipBtnBhv` rows are
  portrait-only; the element reads both heroes' names from the connection's actors, and the
  relationship's own localized name as the value once applied). Before the press the value is
  the authored "unrevealed relationship" - the spoken form of the question mark the game
  shows over the pair (added 2026-08-08, user-caught: the rows read as bare name-pair
  buttons with nothing marking what they are). The pending name the game hides stays
  unspoken. Enter is the game's own press: it commits the pending relationship and plays
  the game's reveal sequence. The apply-all button reads when the game shows one
  (reveal-relationships option, 2+ rows).
- The depart button reads the game's own binding ("Continue", or "Continue: <region>" when a
  destination is set), "unavailable" while relationships are still pending. Enter drives the
  game's keyboard path, which self-validates: with pending relationships the game answers with
  its own reminder dialog (read by the dialog screen) instead of departing.
- Escape opens the pause menu (the game blocks it itself once departure is underway).

---

# Phase 5: The Road (DRIVING mode)

Free driving stays UNCAPTURED - the game keeps WASD (W rolls/cruises, S brakes, A/D steer,
M/I/G/Z/C its own screens). The mod adds a HUD reader and an audio layer.

## 5.1 Free Driving HUD (`DrivingScreen`) - WORKS

Live-verified 2026-07-31 (logical paths and binding suppression; physical keys ride the shared
KeyboardBinding path). The road HUD as Tab panels around a pass-through driving area,
kingdom-map style (Panel root, wrapping Tab). Entry announces "driving" then the biome name
(the minimap's own label; authored "road" when absent). The screen never captures the
keyboard.

- **Driving area** (first stop): every key stays the game's - arrows/WASD drive, M, I, C, Z,
  G, Alt, Ctrl, Escape as shipped. The mod claims only Tab (the game's second minimap key -
  its tab binding rests via an empty binding override the whole stand; M keeps the map).
  Arrows are consumed mod-side so focus stays parked.
- **Status panel**: distance ("7 leagues to Inn") and region ("Regions: 1/3") from the HUD's
  own labels, the flame value (authored "Flame {0}" - the game captions it with the glyph
  alone), armor and wheels (the stagecoach sheet's own stat strings over the live run values,
  damage tooltips in the buffer), and the Loathing meter (named by its own tooltip, "The
  Loathing Abates" + confession in the buffer). The flame's buffer IS the game's hold-Alt
  torch panel (Alt only plays that panel's visual intro, so nothing keyboard-side is lost):
  the state name ("Bright Light"), then each side's effects under the game's own captions,
  read live from the panel's DataContext, which the game re-stamps on every torch change.
  Live-verified 2026-07-31 at flame 88.
- **Party panel**: one element per ribbon hero, left to right as the game draws the strip -
  rank 4 leftmost, the front line rightmost (measured live 2026-08-08: slot 0 sits at the
  highest x), the same direction the combat battlefield row and the crossroads slots walk
  the party - name, HP, stress (the
  game's status-bar strings from the live actor), the shared "New" marker while the ribbon
  shows its notification dot (unviewed sheet notifications, live-verified), every ribbon
  tooltip in the buffer (status effects, "Tense", diseases). Enter is the ribbon's own
  right-click inspect (the hero's sheet - verified it opens the FOCUSED hero). Space grabs for
  a marching-order move: place on another hero's slot runs the game's own drag (the
  hover-index field plus OnHeroRibbonDrag/OnHeroRibbonRelease, so locked-ribbon shifts stay
  the game's), and the landing speaks the resulting order. Live-verified both directions and
  the model commit (TeamPositions), then restored.
- **Goals panel** (only while the game's G panel is open; the game toggles, our tree follows
  by signature): the panel is player-summoned, so the moment its rows arrive focus jumps to
  the first row and the panel reads out (live-verified both ways: the close re-homes to the
  driving area; entering the screen with the panel already open does not jump). Content: the
  biome's mutator and goal sections when the biome has them (unexercised - the Valley has
  neither), and one row per hero goal - hero name through the game's own row-to-party mapping
  (the row shows only a portrait), the goal's own text with progress count, the reward tooltip
  in the buffer ("Reward: candle 2").
- **Buttons panel**: Map (M), Goals & Conditions (G), Inventory (I), Stagecoach (Z) by their
  own tooltip captions, plus the last-chance trophy button (swept inactive, focus follows its
  live state; unexercised - none active this run).
- **Off the driving area** the game's arrow, Space, Enter, and bare-Ctrl bindings rest (same
  override mechanism as the road map - generalized `DrivingKeySuppressor`) so list navigation
  cannot steer, Interact, or flash the glossary; WASD and the letter hotkeys stay live
  everywhere. On-area everything restores (verified in the live input asset, both directions).
  Escape stays the game's pause everywhere.
- Coexistence live-verified: hero sheet (Enter) in and out, minimap over driving (map screen
  takes over, "map closed" then the driving re-announce), goals toggle in and out, the
  stagecoach sheet and inventory keep outranking via the stack. Combat/inn/etc. unaffected
  (mode-gated).

> **Note (goals panel churn):** the panel's sections activate a beat after
> `IsBiomePanelActive` flips (timeline activation), AND row active-states keep flickering
> through the open timeline - a tree built over only-active rows churned rebuilds every
> flicker (each one orphaned focus and re-fired the content-edge jump: "goals, The Valley,
> goals, ..." - user-caught 2026-07-31). The swept set therefore includes inactive rows
> (stable tree, stable signature), elements are reused per row, each label answers null while
> its row is hidden, and the focus jump keys to the game's own open flag - once per summon.

> **Note (held-key re-fire):** applying or removing a binding override re-resolves the whole
> action state, and the game's InputSystem re-fires still-held Button actions in the process -
> the physical G that summoned the goals panel toggled it straight back closed when the focus
> jump engaged the off-area claim mid-press ("the sound twice"; invisible to /input and eval
> tests, which hold no key). User-caught 2026-07-31, probe-confirmed. Suppressor transitions
> now wait out any held toggle-class key, retried by the per-frame reassert/restore loops;
> WASD and arrows deliberately do not defer (their continuous actions re-fire harmlessly, and
> cruising holds W).

## 5.2 Road Audio Layer and Transients (RoadSense) - WORKS

Cues live-verified by ear 2026-07-25 (pickup ping) and 2026-08-04 (turning). The mod's own
NAudio output (independent of FMOD; `assets/audio`, placeholders replace 1:1). The cue
roster is deliberately minimal - pickups are the only road object worth steering at, so
only they and the coach's own motion sound (pickup ping, turning, turn-end, road edge);
collection is the game's own sfx plus speech, and everything else on the road is speech.

- **Every uncollected loot pickup in range loops** (one live loop voice each - louder as it
  nears, parameter steps smoothed ~5 ms against zipper noise), re-aimed EVERY frame so
  steering reflects immediately. A pickup means a loot-granting drive-through event
  (`TriggerItemBhv`); the all-biome prefab audit (2026-08-07, every shipped OBJECTS event
  loaded and inspected) found the final climb's mountain "pickups" are loot-less
  destructible debris - those stay silent, and the hazard-skinned city events (iron spikes,
  corpse, burning books) ARE loot and ping normally. Distance and pan run hitbox to hitbox - the closest points between
  the pickup's trigger colliders and the coach's solid colliders (control rig, horses,
  wagon body) - so a wide zone whose edge is dead ahead sounds centered, touching reads as
  distance zero, and the coach's own width counts against the gap; a center-distance bound
  pre-culls far candidates before any physics query. A loop cuts the frame its pickup is
  collected or drops out of range (a 10% exit margin keeps the boundary from flapping). The
  allocating scene sweep runs on a 0.7 s clock to refresh the candidate array and its cached
  collider sets (measured live: ~43 pickups loaded, 2 within the 80-unit range - a handful
  of concurrent voices, mixed under one output limiter).
- **Road edge** (wired 2026-07-25): off-center distance against the road's half-width from
  the game's own road geometry; bumps panned to the drifting side past 85%, re-arming
  under 70%.
- **Turning** (wired 2026-08-04; player ear-passed same day, which caught the mirrored pan -
  the game's positive turn ratio is a LEFT turn): a loop while the coach actually rotates,
  panned toward the turn and louder the harder it is, with an end cue on the settle back to
  straight. The signal is the coach's own turn-speed state times the speed ratio (the
  product its rotation math applies, and what the horses' turn animation runs on), so
  road-snap curves the coach steers itself sound too; start over 25% strength, end under 12%,
  the gap against micro-correction chatter. A capture or mode change cuts the loop silently -
  the end cue marks only a real settle.
- **Pickup titles ride the loot toast** (`EventLootToastPresented`): a road grant never raises
  the inventory widgets' loot event (the mod's original hook, dead code on the road - found
  live 2026-07-25 as "collection sound but no name"), so the item's own title speaks when the
  game's corner toast presents - speech only, no mod cue, because the game's own pickup sfx
  already marks the moment.
- **Auto collect** (wired 2026-08-07, awaiting live verification; the "auto collect pickups"
  toggle in the mod settings tab, default off): the pings fall silent and a pickup collects
  itself as the coach passes it abeam - first seen clearly ahead, then within the road's
  width (`GameConstants.ROAD_SIZE`) as its forward reach crosses zero - by invoking the
  pickup's own `OnTriggerEnter` with a coach collider, so the game's whole drive-over
  sequence runs unchanged (its interaction gates, vfx and sfx, the loot grant, the corner
  toast that already drives the spoken title), with the balancing `OnTriggerExit` invoked
  once the event completes. Only loot-granting drive-through events qualify
  (`TriggerItemBhv` present, `DRIVE_THROUGH` interaction - an OBSTACLE event would
  force-stop the coach); other routes cannot be grabbed because the road width is far under
  branch separation and the game itself deactivates other branches' events on route
  selection. A pickup the coach physically rolls over stays the game's own collection (the
  synthetic enter is skipped while a real collider sits in the zone). The pass detector
  keeps running while a screen holds the keyboard, so a pickup passed as the coach coasts
  under a just-opened panel still collects.
- Speech-only road lines: road damage speaks the combat damage wording (the coach's
  stop/start is left to the game's own driving audio); a junction's banners coming up speak
  "fork ahead" (once per junction).
- **Road transients** (toasts live-verified 2026-07-31; barks rewired 2026-08-08): tutorial
  and message toasts route by mode through the toast postfixes (combat queue in battle, the
  road pending queue on the road; the patches attach at startup, not on the first combat
  resolve). Road barks speak speaker-prefixed through postfixes on the bark spawner's two
  overloads (`BarkEvents`) - the one choke point every road bubble passes - because banter
  act-outs and relationship exchanges spawn straight from the hero ribbon and NEVER raise
  the bark event (found 2026-08-08: only queue-path reaction barks did, so banter had been
  silent). The patch also picks up road-event reaction, node-approach, and pet-cage barks
  (the pet's rides the world-anchored overload, no speaker prefix); combat bubbles run the
  same spawner, so the patches gate on the DRIVING mode and battle barks stay with the
  combat module's bark-event listener. The coach's Loathing-resist pop speaks the
  game's own "LOATHING RESIST" text (the English template carries no number slot); the
  low-flame ambush pop ("The Flame Exhausted") rides the combat pending queue outright,
  because it plays as the ambush battle spins up, and so speaks with the battle's opening
  (queue-level verified; a real ambush is unexercised).

## 5.3 Road Map (`MapScreen`, M while driving) - BUILT

Deployed 2026-07-25, awaiting live verification. The game's minimap overlay, which does not
pause the coach - so the screen SHARES the keyboard instead of taking it: our arrows walk a
map cursor (the game's arrow bindings are disabled with empty binding overrides while the map
stands, re-asserted per frame and restored on close), WASD keeps steering, and M / Z / Escape
stay the game's own (Escape closes through the game's handler; the mod speaks "map closed" on
any close).

- The cursor (STS2-style tree walk over the minimap's own node/link graph) starts at the
  **wagon**: on its node when the coach stands at one ("at Assistance Encounter, traveled"),
  else a synthetic between-nodes position read live ("on the road, Gate to Hoarder"), since
  the coach keeps moving. Up crosses one road per press ("road, node"; a fork's first
  alternative is prefixed "choice"); Left/Right swap among that fork's alternatives; Down
  retraces the exact path taken, then the traveled road, then back onto the wagon. Home jumps
  to the wagon, End to the biome's last row.
  Auto-advance through no-choice stretches was tried and removed; it is planned to return as
  an opt-in setting.
- **Fog of war is enforced by construction**: every node and road name reads through the
  game's own fog-gated tooltips (`MinimapIcon.GetTooltip()` returns the "Unknown" tooltip
  until revealed; roads read the unknown-route tooltip until `IsRevealed()`) - an unscouted
  node never leaks its true type.
- Node line: fog-gated name, then candle/loathing/contract markers (the sighted overlay icons,
  read from their live objects) and traveled/not-taken state. The buffer holds the full
  tooltip, marker tooltips, row position, and one line per road out ("Barricade combat, to
  Lair..."). The wagon's buffer carries its road's route line and row position.

**Known gaps:** phase 1 walks the current biome's ladder (biome boundaries hand over only
where the game links them); reveal events (scouting, watchtowers) are not announced as they
happen; no points-of-interest jump or user markers yet (the STS2 features staged for phase 2);
the whole screen is awaiting live verification.

## 5.4 Fork Menu (`RouteChoiceScreen`) - BUILT

Not yet reached in play. Opens when the game's own junction wait halts the coach unchosen.

- Routes in left-to-right order read "direction, destination" (the game's road-indicator
  titles; "Unknown" unrevealed - the hidden type is never leaked). Buffers: description,
  which heroes prefer the route, banner tooltips.
- Enter commits via the banner's own OnClick (game audio + narration; the coach then drives
  itself); Escape dismisses that junction back to manual steering (steer at a banner holding
  W, the game's hold-to-fill).

## 5.5 Confession Select (`BossSelectScreen`) - BUILT

Verified on a dev-shown instance at an inn; the real road trigger is unexercised - it fires
once per run, early in the drive. The "Complete your Confession" screen.

- One element per confession option (the game's own labels; locked confessions carry the
  game's "???" placeholder, which the text filter already reads as "unknown"), then the
  confirm button - icon-only in the game with no tooltip, captioned here with the game's own
  `continue_label` string. Enter on an option is the game's own submit (marks it, arms the
  confirm, reads back "selected"); confirm commits the confession and the drive resumes.
- Escape is deliberately inert, like a road story: the choice is mandatory.

> **Note:** Before this screen existed the generic floor took the surface, whose Escape is
> `TryCloseScreen` - a player escaped past the choice, and a run without a confession has no
> `RunManager.Boss`, therefore no Mountain route: the last inn's Select Route screen is
> genuinely empty, embark can never arm, and the game's own forced-run-end detection also
> assumes a boss, so the run dead-ends with End Expedition as the only exit (observed live
> 2026-07-24).

## 5.6 Stagecoach Sheet (Z, read-only `WainwrightScreen` variant) - WORKS

Live-verified on the road 2026-07-31: entry announce, full walk (name, stats, all seven slots
with their unlock/lock tooltips in the buffer), Escape. The same class as the inn's Wainwright
(8.5); the screen is named by the game's own per-context title ("The Stagecoach"), derived
from the game's loc keys on the entry frame since the sheet stamps its DataContext a frame
after topping the stack. No wallet, repairs, or livery here, and slot edits are refused by the
widget's own editable gate.

## 5.7 Player Inventory (`InventoryScreen`) - WORKS

Live-verified 2026-07-25 on the road: entry landing, tab, and the Escape close path back to
the underlying screen; the panel body is the inn's live-verified reader extracted verbatim.
Previously fell to the generic floor, which announced itself as "Sort" and read item slots as
their bare stack count. The game's inventory screen as pushed on the road, at the crossroads,
and from the loot screen.

- Nothing but the shared bag panel (`InventoryPanel`, the same reader the inn hub embeds): the
  filter as a tab (Left/Right apply the game's own icon-only filter buttons, captions from
  their loc keys), slot count ("15 / 20") and wallet rows (Relics/Mastery/Baubles, captions
  from their tooltips), the sort button (press confirms "sorted by type"), one element per
  carried item (title and stack, full tooltip in the buffer, Shift+Enter discard), the free
  capacity as one line, and Space grab-and-place with Shift+Space single placement. Escape
  drops an armed grab first, else the game's own `HidePlayerInventory` close. Full panel
  detail in 8.2.
- The inn outranks this screen by registration order and keeps its inline copy; dedicated
  station screens above both take their own surfaces.

---

# Phase 6: Roadside Nodes

## 6.1 Node-Arrival Prompt (`EnterNodeScreen`) - WORKS

Live-verified 2026-07-31 (synthetic opens; a real node arrival is organic). The one screen
every roadside stop halts on (`EnterNodeScreenWidgetBhv`), named by the authored "road stop".

- A single button reading the interaction's own loc key through the push params ("Search the
  Cache", "The Field Hospital"), with the authored "candle reward" value while the game shows
  its icon-only candle marker (entering feeds a hero goal).
- The push params land a frame after the object tops the stack, so the entry reads a bare
  "button" once and the label's arrival requests the one re-announce (the shared LabelArrived
  pattern). Enter is the button's own press; the game refuses to close the prompt, so Escape
  answers "unavailable".

## 6.2 Node Types and Where Each Lands - WORKS (mapped 2026-07-31)

Every node executes a serialized trigger list, and every interactive one funnels through ONE
shared prompt - `CommonUiBhv.ShowEnterNodeScreen` (6.1) - then branches to its surface:

- **CACHE / CACHE_GANG** -> prompt -> the loot screen (7.3). Covered, player-verified.
- **STORE (the Hoarder)** -> prompt -> the shared store surface (6.6). Covered, live-verified.
- **STORY_ASSIST / RESIST / COSMIC / CULTIST (+ gang/coven variants)** -> prompt -> the story
  screen (6.3), sometimes into combat and loot. Covered surfaces; story commits mutate the
  run, so exotic variants are left to organic play.
- **STORY_HERO (Shrine of Reflection)** -> hero story intro (6.4) + story combat. Covered,
  player-verified. **STORY_HERO_REPLACEMENT** -> the replacement-hero screen (8.9). Covered.
- **WATCH_TOWER** -> prompt -> `TriggerScouting.StartScoutPulse` only: NO further UI - the
  reveal reads through the road map. Effectively covered.
- **HOSPITAL** -> prompt -> the Field Hospital (6.5).
- **DUNGEON / GUARDIAN / CREATURE_DEN / kingdom gang bosses** -> prompt -> combat chains
  (Phase 7) - not testable-with-undo, entering commits a fight. The advance-or-escape dialog
  between a chain's battles is 6.7.
- **OASIS / GATE / BRIDGE** and the kingdoms node skins ride the same trigger set (prompt +
  effect/loot/story/mode triggers); no bespoke screens found in code.

> **Note (synthetic-test rule, learned the hard way 2026-07-31): wait for the run to settle
> before any synthetic screen push.** The run's managers (`GameTypeMgr.RunValues` and friends)
> start a beat AFTER `CurrentMode == DRIVING` flips; a synthetic push inside that gap stalled
> the run start for the whole session (dead RunValues, the game's own UpdateHealCost NRE-ing,
> "N/A" costs everywhere) - broken until restart, with no log line. Gate on
> `RunValues != null`, not just the mode. Both the prompt and the Field Hospital open
> synthetically via code and close through their own teardown (probed live; `ShowHospital`
> additionally reveals the player-inventory entry it raises beneath - one extra close in a
> synthetic test).

## 6.3 Road Stories (`StoryScreen`) - WORKS

Live-verified 2026-08-05 on "Assist Us!"; the commit event is wired but deliberately not
pressed in testing.

- Every road story's choices are heroes; each reads the choice itself: the hero's name, then
  the button face (bark line, quirk gate, relationship banner when bound) and the sighted Alt
  panel's previews grouped per side ("Bigby: Life's luxuries are wasted here, party, Relics
  -12, Flame 30"). The buffer reviews it per line: the hero's vitals (name, HP, stress), the
  bark, then one line per preview ("party, Flame 30"), split party/enemy.
- S glances the focused choice's hero vitals in place (Story input category, live only here);
  off a choice the key is silent.
- Enter fires the game's own selection event (the click-and-hold equivalent), honoring its
  hoverable gate; C inspects the hero. The narration itself is the game's voiced narrator,
  already audible.

**Known gaps:** choices spawning after screen entry leave focus on the utility buttons until
the player moves (Home reaches the choices); story RESULT presentation is unread beyond the
narrator; affinity change previews unspoken.

## 6.4 Hero Story Intro (`HeroStoryIntroScreen`, HERO_STORY_INTRO mode) - BUILT

The chapter card shown at a shrine before a hero story resolves; the mode previously had no
screen at all (silent, keyboard released).

- One readout: the chapter title leading (from the game's `hero_story_title` binding, which
  lands after an async portrait load - the entry announce falls back to the hero's name and a
  re-announce fires once the title binds), the hero's name, and the chapter body text as
  buffer lines. The game's own narrator voices the title and, on story chapters, the body.
- The Continue button appears only after the presentation and narration finish (the game's
  fade-in cue); its appearance is spoken. Enter drives the uGUI submit path (the game's own
  click handler); the game gates it with its own input-enabled flag.
- Escape is inert: the game itself blocks the pause menu in this mode, so silence matches the
  sighted experience.

## 6.5 Field Hospital (`HospitalScreen`) - WORKS

Live-verified 2026-07-31 on a real run (synthetic open, damaged party for real costs; an
actual node visit and a treatment purchase remain organic; the same class should serve the inn
physician - unverified there). The road node's `HospitalScreenBhv`, named by its own composed
title ("Field Hospital: Triage" / Wellness / Pharmacy - it retitles per tab).

- Layout: the hero pager (Left/Right page the party through the browser's own stepping, name +
  HP + stress, status tooltip in the buffer), the tab selector (the tab buttons' own captions;
  the game disables the active tab's button, which is how the current one reads; Left/Right
  click the game's own buttons), then the active tab's rows.
- **Triage**: the cure-disease button (its own texts; "Upgrade Physician to unlock" rides the
  tooltip into the buffer), then minor and full heal - each reading its own amount label
  ("+8 HP", "+MAX") with the price composed from the model exactly as the store composes it
  (CostDescription, strikethrough off - the game's own bound text carries a crossed-out
  original price that would read as two numbers).
- **Wellness**: treatable quirks by their own names ("selected" on the one the commands would
  treat; Enter is the row's own click, spoken back), then the lock/remove commands captioned
  by their tooltips with the cost following ("Remove, relic 16") - the visible row is icon
  plus cost only. The game's "No Treatable Quirks" notice reads when shown. Rows are reused
  per button across the game's per-selection rebuilds so focus never re-homes mid-flow.
- **Pharmacy** hands the surface to the shared store screen (the embedded `StoreUiBhv` matches
  it); Escape there returns to Triage through the hospital's own first tab - the embedded
  store's done button is a husk (it only hides the inventory panel and would strand an empty
  store; found live, fixed in the store screen's back). Escape on the hospital itself closes
  through the widget's own close-button handler.

## 6.6 Road Merchants - the Hoarder (`StoreScreen`) - WORKS

The game raises the player inventory panel above the `StoreUiBhv` screen and the pair reads as
one store surface, named by the store's own title: wallet rows, store slots with price and
stock, the bag with sell-per-press where the game allows selling (the Hoarder needs the
altar's Enable Hoarder Selling option). Escape exits through the store's own done flow, which
resumes the drive. Full store detail in 8.3 (the same screen serves the inn Provisioner).

## 6.7 Lair Advance Dialog (`LairAdvanceScreen`) - BUILT

The advance-or-escape dialog (`DungeonConfirmationDialogBhv`) the game raises between the
battles of a multi-battle roadside node - lairs (the Library and its kin) and guardian nodes
share it, retitling through its own `battle_advance_*_confirmation` loc keys. Named by the
dialog's own title text (set directly, not databound, so the entry read never races a bind).

- Reads as a modal: the description, one row per party ribbon (the shared `HeroRibbonElement`:
  name + HP + stress, ribbon tooltips in the buffer, Enter = the ribbon's own right-click
  inspect, which the game allows here on mouse+keyboard), the reward icons grouped under the
  authored "looted" (secured by the cleared battles) and "next battle" (the next fight's
  offer) section labels - each icon's name and stack from the widget model like the kingdom
  panels' rewards, tooltips in the buffer - then the two choice buttons by their own captions.
- The sighted commit is a one-second pointer HOLD on either button (`Submit`/`ExitMenu`
  press-and-hold fills; the onClick path is unwired - the recurring hold-class commit). Enter
  drives the widget's own `OnConfirm`/`OnDecline`, which invoke the game's stored commands and
  close the screen; the surface that follows (the next battle, or the results flow) announces
  itself. The game hides the escape button when escaping is not offered, and the sweep honors
  that.
- Escape answers "unavailable": the game refuses to close the dialog without a choice, and
  folding Escape into the escape commit would abandon the lair on a reflex keypress.

**Known gaps:** built offline, not yet seen live; the two buttons' captions may activate a
beat into the open animation (the landing is the code-set description text, which is safe).

---

# Phase 7: Combat (COMBAT mode)

## 7.1 Combat (`CombatScreen`) - WORKS

Live-verified 2026-07-24: two full rounds fought to Victory - skill picks, target picks,
kills, turn handoffs, free-action stance swap - with the expanded event set and header row.

### 7.1.1 Layout - WORKS

Top to bottom:

- **Header row** (Left/Right within it):
  - The battle status ("round 1, Audrey"; torch value, flame state and effects, wave count,
    round detail, and retreat odds as buffer lines). The flame lines mirror the flame's
    hover panel - the state name ("Bright Light"), then each side's current effects under
    the game's own Heroes/Enemies labels, read from the widget's data-bound values (the
    game re-stamps them on every flame change; a mid flame grants neither side anything and
    reads only the state name). A token glyph in the effect lines gets its glossary line
    ("Blind: 50% chance to miss next attack"). The retreat tooltip's Loathing/stress cost
    lines read as the game writes them; the game offers no description of either on this
    surface (the Loathing meter with its tooltip exists on the driving HUD and inn results,
    both read there), so none is spoken - live-verified 2026-08-10. The wave count mirrors
    the game's pip strip beside the
    round counter, both of its sources: chained battles ("battle 1 of 2" from the
    scenario), and summon-controller wave fights (the `wave` configuration is the only one
    the game ships with the display on; "battle 1 of 2" while the enemy wave queue still
    holds, "battle 2 of 2" once the last wave is in - added 2026-08-08, unverified live,
    needs a fight that uses the `wave` summon config). A fight with no waves speaks no
    line, like the pipless single battle.
  - The **turn order** ("turn order, Sahar, Audrey, Widow...", current actor first, read live
    from `QueryTurnOrder`; the order is rolled per round, so the current round's remainder is
    all the information the game itself has). Its buffer holds just one combatant name per
    line so review steps the order name by name. A name shared by several living enemies speaks
    with its rank - "Lost Soul 1, Lost Soul 3" - matching the game's only pointer to the
    specific one (the model highlight under a hovered portrait); the numbers are read live, so
    they follow deaths and position changes, and drop when one survivor remains.
  - The **battle goal** (the game's `battle_goal_<config>` string, present only in fights that
    carry one).
  - The **battle modifier** (title from `battle_modifier_title_<id>`, present only in fights
    that roll one; its tooltip title and effect/buff descriptions are buffer lines).
    Live-verified 2026-08-02 ("Rampaging Beastmen!" with "Combat Start: Enrage" in the
    buffer).
  - The **gang escalation** (Kingdoms combat only): the ribbon tooltip's own title
    ("Escalation 1") with its effect lines as the buffer, composed by the game at battle
    start (sighted access is the More Info hold). Live-verified 2026-08-02 in a Drakia
    siege.
- One battlefield row laid out like the screen (restructured 2026-08-08, awaiting live
  pass; previously two rank-ordered rows): the party right-to-left - rank 4 leftmost, rank
  1 at the front line - then the enemies rank 1 to 4 continuing rightward, so the two front
  lines meet in the middle and Left/Right walks the whole field the way it looks. The row
  is one flat container, so crossing the meeting front lines is plain list adjacency
  (flattened 2026-08-08, user-caught: the nested per-team strips made Left from the enemy
  front line enter the party strip at its remembered-else-first child - a jump to rank 4
  whenever a turn rebuild had cleared the strip's memory). The per-team readers (team
  buffers, glances, the target snap) filter the row by each element's side - the
  position IS the side (labels are name + Rank + HP read
  live; a name shared by several living enemies carries a stable ordinal - "Lost Soul 2" -
  on focus, in the team buffers, and in the glances alike. Ordinals count 1..N in
  first-sight position order, not by rank (changed 2026-08-12: two Widows at ranks 3 and 4
  read Widow 1 and Widow 2): a position shuffle never renames anyone, a death compacts the
  survivors down (Widow 2 becomes Widow 1), and a sole survivor drops the number; the
  first-sight order is the one remembered piece, reset when the battle ends. A monster's
  name is its data id's loc string, the same source as the game's turn-order tooltips.) Corpses and prop monsters (battle-complete classes) are in the strips like any
  combatant - they hold a rank, take hits, and are targets for corpse-clearing - matching the
  game's own hoverable battlefield entities (unverified live). Kingdoms militia allies
  (`kingdoms_ally` classes fighting AI-driven in the party's line) are in the party strip the
  same way - the game's character sheet excludes them from its pager only because they have
  no hero sheet (unverified live, needs a siege). The row rebuilds whenever its ordered
  combatant guids change, not just on turns and count changes - a battle-start shuffle
  (the Faceless Visage trinket moved the party after the entry build, leaving the QWER
  glances answering the pre-shuffle arrangement), a mid-turn reposition, a death or summon
  all re-sort it the frame they land - and every rebuild re-lands focus silently on the
  same combatant instead of dropping the cursor at the row's left end.
- The skills row (horizontal), with the game's own "Uses: N" limit text and the game's
  `invalid_skill_reason_<type>` wording when a skill cannot be used - wrong rank, cooldown,
  out of uses - instead of a bare "unavailable". When the game grants an always-equipped copy
  of a skill the player also equipped, it shows two identical buttons that select the same
  skill - the mod reads only the first and ends its buffer with "also granted as a bonus
  skill".
- The commands row (Move, Pass, and Retreat when the game offers it).

### 7.1.2 The Turn and Targeting - WORKS

- Enter on a skill runs the game's own pick handler; focus then snaps to the first valid
  target (enemy strip first, so a hostile pick lands on an enemy and a friendly one on the
  party) and the landing line - with its preview - is the whole announcement. Arrows browse
  on from there as usual. Live-verified 2026-08-04 (Crush onto the enemy strip, Move onto
  the party strip, via /input).
- Landing on a combatant then plays a validity beep (660 Hz triangle for a valid target,
  440 Hz for an invalid one, `assets/audio/combat`), only when the validity CHANGED from the
  previously focused combatant - runs of same-validity targets stay silent.
- An invalid target's line leads with the derived reason (out of range, allies only,
  stealthed..., mirroring the game's own target-validity walk, which sighted players see only
  as dimming). A valid target's line ends with the game's own precomputed preview
  (`QuerySkillPreview`: "85% hit, 5% crit", or the heal range on friendly skills;
  "intercepted by X" when a guardian will absorb the hit, "riposte 3-7" when the pick draws a
  counter; "removes / steals / converts X" for the recipient's tokens the pick would strip -
  the lists the game previews by flashing tray icons, named only when held. Dot cleanses have
  no game preview and are deliberately not spoken, keeping parity with the sighted view - the
  removal reads in the skill's own text; a conditional heal below its HP threshold previews
  as 0 and stays silent, matching the game's hidden heal bar segment).
- Enter on a target sends the game's own actor-pick event to execute. Escape cancels
  target-select first, fully deselecting the pick through the game's own deselect event
  (the bare cancel keeps the skill armed for the mouse flow, which left Enter refusing to
  re-pick it) and landing back on the skill's button, whose plain line ("Crush, button")
  is the whole feedback - Enter picks it again cleanly; else Escape opens the pause menu.
- Turn lines ("round 2, Audrey") are spoken outright on every turn change - focus can sit
  anywhere - and logged to the combat buffer once.
- Battle start holds the entry announcement until the first turn settles (the screen
  resolves mid-handoff, when the header still reads empty) and lands focus on the header's
  battle status, so entry reads "combat" then "round 1, Audrey" once - never a strip slot
  plus a separate turn line (live-verified 2026-08-08). If the hold cap ever expires
  first (the router logs "entry never settled"), entry falls back to the strip landing with
  the turn line spoken on settle.

### 7.1.3 Glance hotkeys - WORKS

Live-verified 2026-08-04 (player-pressed, all three layers; remapped 2026-08-08 to the
battlefield row's left-to-right order). One key per row slot - 1-4 the enemies (1 = their
rank 1), Q/W/E/R the party (Q = the backmost, R = the front line) - spoken in place, focus
stays put. A slot with no combatant is silent. The keys live in the Combat input category (declared by this
screen and the inspector overlay only), so they never shadow the roster rename or other
screens' keys; all 24 are rebindable from the mod keys tab.

- **Bare key**: the scroll-over read without the rank word (the key names the row slot) -
  "Lost Soul, HP 13/13", "Bigby, HP 40/40, Stress 0/10" - including the target-validity
  reason/preview while a pick is pending.
- **Shift**: the token/dot/buff summary, positives then negatives, each token as its bare
  name with the stack count when above one (mod-authored "{0} x{1}" - the game's own format
  lacks the space a reader needs) and its duration when above one ("Death Armor x2",
  "Block (3 Turns)"); dots as the game's condensed dot text composed per dot type (the
  game's composer serves one type per portrait icon; fed mixed types it merged them into
  one line labeled by the first, which is how a death-blow regen vanished under a bleed -
  user-reported 2026-08-09), healing dots (regen) sorted with the positives; combat buffs
  (the `IsEligibleToShowAsCombatUi` set) split by their buff/debuff tag. No effects =
  silence.
- **Ctrl**: the resistance grid as one line, using the game's grid names with the shared
  RESIST word stripped (`CommonAffix`, language-agnostic character-level affix strip):
  "STUN 20%, BLIGHT 40%, ... DEATHBLOW 90%".
- **S**: the acting combatant's status glance, no strip key to hunt for.
- **T on a focused skill**: every combatant the skill could take right now, each with a
  terse preview ("Lost Soul, 100% hit, 5% crit, 4-6 DMG; Woodsman, ...") - the game's own
  precomputed valid-target entries (`GetValidSkillTargetEntries`), read without picking;
  resist chips and removal lists stay per-target after the pick. A skill with no valid use
  speaks its grey reason; anywhere off the skill bar the key is silent.
- **Shift+T**: the header's turn-order line from anywhere in the battle.
- **A**: the telegraphed affinity changes (the icon the game shows on the responding hero) -
  on a focused skill every valid target's, in the announced-change form ("Dismas and Audrey,
  affinity +1"; a per-target change carries the target's name first); on a focused combatant
  while a pick is pending, that pick's change against them. Nothing telegraphed = silence.
  The chord is shared with the inspector's combatant cycling - the cycle acts only inside
  the inspector view, the glance only on a skill/target focus. The same change also closes
  the per-target preview itself (the target-select landing, the slot glances, the team
  buffers' overview lines) - the hover moment where the sighted icon appears - so A is the
  re-check and the per-skill view.

### 7.1.4 Buffers - WORKS

- Combatant buffers: HP, stress (heroes), then one line per token (hidden tokens filtered by
  the game's own `IsHidden` gate - they are internal logic-control state whose loc text is a
  "please file a bug" placeholder), per dot, and per combat buff (filtered to
  `IsEligibleToShowAsCombatUi`, e.g. Preparation's "On Riposte: heal Self 10%"), all from the
  game's own describers. Buff lines honor the game's `buff_tooltip_<id>_override` strings
  with the same precedence its tooltip composers apply (fixed 2026-08-12: the Weapon Rack's
  positive-token immunity is named only by that override - its stat has no formatted loc
  string, so the per-buff describer leaked "actor_stat_type_formatted_resistance_positivetoken",
  the same raw key the game's own panel shows; it now reads "Cannot Gain Positive Tokens").
- Skill buffers: the full skill card (shared `SkillCard` composer with the hero sheet),
  then any affinity change the skill telegraphs (`QuerySkillPreview` per valid target,
  gated by the game's own `m_IsTelegraphed`): one shared line when every target telegraphs
  the same change, else one line per telegraphing target, that target's name first. The
  trailing token-glossary lines resolve path-variant tokens through the skill's own id
  (fixed 2026-08-12: a hero path swaps a token for a suffixed variant with its own
  description - the Duelist's stances become `dul_*_stance_p<n>` - while the skill text
  keeps the base glyph, so an Antagoniste Duelist's cards glossed the base stance text; the
  skill id carries the same `_p<n>` suffix and now picks the variant. Applies wherever a
  skill card is read: combat bar, hero sheet, trainer, the inspector's studied skills.)
- The **enemies** and **party** buffers (unverified live): one overview line per living
  combatant in rank order - name (with the target-validity reason while a pick is pending),
  rank, HP, a hero's stress, the pending pick's preview, and the token/dot/buff summary the
  Shift glance speaks - readable from anywhere in the battle without moving focus, pad
  included (the glance digits are keyboard-only).
- The **hero** buffer follows the focused element's hero: a skill or a friendly combatant
  binds its owner's identity (name, class, path), HP/stress, and speed.

### 7.1.5 Battle Events - WORKS

Announced as they happen (queued, so narration stacks in order) and kept in the **combat
buffer** (Ctrl+Left/Right; follows the latest line; empties when the battle ends). Display
gates mirror the game's own pop-text handlers. Names use the battlefield form, so a
duplicated enemy carries its stable ordinal ("Lost Soul 2 took 4 damage") in every event
line, as does the interceptor named by a guarded pick's preview. Covered:

- Damage taken ("Lost Soul took 4 damage"; number dropped at 1; ", crit" appended on crits),
  heals (with crit variant), misses and dodges from the finalized skill results ("Woodsman
  missed Paracelsus" / "Audrey dodged").
- Stress damage and relief ("Dismas gained 2 stress" / "Audrey lost 1 stress"), meltdowns (the
  game's "resolve is tested" line plus the outcome's own name), deaths, death's-door falls and
  survivals ("Woodsman resisted the death blow").
- What AI combatants do ("Lost Soul used Chomp on Paracelsus") - keyed to the performer's
  controller, so kingdoms militia allies announce their actions too - never the picks of a
  player-controlled hero.
- Token, dot, buff, and quirk applications ("Dismas gained Crit", the game's own names and
  count format, honoring its pop-text visibility gates; buffs speak their stat text), token
  consumption and negation ("Sahar spent Speed" / "Sahar lost Weak"), resisted effects
  ("Woodsman resisted Blight"). A token the library does not define, or defines as hidden,
  never speaks in any of these lines (fixed 2026-08-08: skills apply library-less logic
  markers like "token_logic_temporary" whose ids leaked raw into the gained line - the same
  IsHidden gate the combatant buffers always applied). A visible token with no glyphed name
  entry speaks by its plain `token_name` string instead (fixed 2026-08-12: the Violinist's
  song-part markers define only the plain name, and the gained line leaked the raw id - which
  also spells out the riposte mechanic the game's own strings never mention; the marker now
  reads "Last Played" everywhere, the song part staying in the combatant buffer's tooltip
  line, exactly the sighted hover); the target preview's removes/steals/converts names take
  the same fallback.
- Retreat outcomes, wave starts, and the final round (all three via the game's own pop-text
  strings), wounds, affinity changes ("Dismas and Paracelsus, affinity +1"), barks ("Dismas: I
  line 'em up..."), hero objective completions, and tutorial/message toasts shown over combat
  ("tutorial, Enemy Death Armor"; Harmony postfixes on `ToastManager`, the one toast surface
  with no event).

Verified live 2026-07-24: turn order readout, blank goal hiding, buff buffer lines, crit
damage, miss, stress damage and relief, token spend and loss, death-blow resist, affinity
tick, barks, tutorial toast, always-spoken turn lines, wave count suppressed in a
single-battle fight.

Hover parity (2026-08-03, deployed, awaiting live pass): the target preview line grew the
panel's other two calculations - the effective damage range with every live modifier folded
in (flat crit damage on a guaranteed crit, the game's own switch) and the tested resistances
after the attacker's piercing ("Blight RES 40%", the chips the panel highlights). An enemy's
buffer opens with its monster type tags and speed (the hover panel's identity facts, shown
nowhere else - the inspector already spoke speed); a combat item on the bar reads the game's
own "Quantity: N" beside its remaining limit uses.

Ordainment (live-verified 2026-08-07): an ordained combatant - the game's blessed portrait
icon, `ActorInstance.IsOrdained` - leads every spoken line with the game's own term
("ordained, Pillager Hatchetman, Rank: 1, HP 22/22"; the focus line, the overview lines in
the enemies/party buffers, and the bare glance alike), and its buffer carries the icon's
tooltip after the identity facts: the game's ordainment header, then the modifier's rolled
effects line by line, sprite glyphs read as words.

**Known gaps:** dodge/heal/meltdown/retreat/final-round/wave-start/wound/quirk/objective/
message-toast lines are deployed but not yet observed live (their handlers share gates and
composition with the verified ones); the goal readout is unverified in a fight that has one;
relationship skill markers rely on the skill card's actor-aware result strings and are
unverified with an active relationship; a combat item rides the skill bar as a regular skill
button but no hero had one equipped to verify; a token id with no name key anywhere
("blind-line") reads as its humanized id; Move is untested against position targeting; Pass
briefly announces "select target" before auto-resolving; the retreat element only (dis)appears
on turn-boundary rebuilds; stealth/summon edge cases and the corpse and militia rows
unexercised; the inspector reads a militia through its hero-shaped branch (the game's
academic view has a dedicated militia section showing their one combat item) - needs a
militia fight to design against; target
beeps, invalid-target reasons, and the hit/crit preview are unverified against friendly skills
and stealth (guard interception and riposte verified live against the preview cache, spoken as
suffixes); death lines follow the game's death presentation, so the battle-end cleanup that
sweeps leftover corpses off a finished team (Detach) and capture teardowns (None) stay silent
while mid-fight corpse kills and decay speak by the mod announcements tab's corpse-deaths
toggle (default on).

## 7.2 Inspector (`AcademicScreen`, over combat) - BUILT

The game's academic view (hold-Alt / middle-click for sighted players), driven through the
game's own show event so the camera, fog of war, and its gates follow.

- **I** toggles it on the focused combatant (the acting hero when focus is not on a combatant;
  "unavailable" when the game refuses - enemy turns, mid-animation). **A / D** cycle
  combatants battlefield-order without leaving the view (the game's own keys for it); the new
  subject's name is spoken and focus keeps its row. Enter (or C) on a party hero's identity
  line opens their character sheet; Escape or I closes, and the game's own force-close (combat
  resuming) falls back to the combat screen, which re-announces.
- Layout, top to bottom, all read live from the model through the game's own describers:
  - The identity line ("blessed" leading on ordained enemies, then name, HP, stress on
    heroes, speed; death's door and the boss-blessing description as buffer lines).
  - The studied **skill list**. Enemies: round skills first, then turn skills, each with the
    game's own token-view card in the buffer - ranks/targets, the tokens and dots it applies,
    melee/ranged - plus flavor description, token ignores, and use conditions; the full effect
    renderer is a player-skill surface whose enemy-only internals, AI class changes, read as
    raw ids; skills the player has never seen use the game's own "???" hidden strings.
    Heroes: equipped skills with the full skill card, remaining uses and cooldowns.
  - Hero **conditions** (class conditions, condition-tagged buffs, stagecoach effects, the
    wound line; the row vanishes while the hero has none, like the status rows - the game's
    own view blanks its conditions binding then, live-verified 2026-08-06 on a story-battle
    Highwayman), **trinkets** (enemies and Kingdoms allies carry visible ones).
  - The **resistance grid** (every resist with the game's immune and death's-door special
    cases; per-source breakdown in the buffer).
  - **Tokens, damage over time, buffs, debuffs** (empty sections vanish; hidden tokens are
    filtered out, the same IsHidden gate the game's own token icons apply).

**Known gaps:** unverified against an ordained (blessed) enemy, a stealthed enemy, and
Kingdoms militia allies; resist percent formatting assumes the model's 0-1 fractions.

## 7.3 Victory / Loot (`LootScreen`) - WORKS

Live-verified 2026-07-23: item buffer, single take, leave-items dialog, last-item auto-close.
A battle's Victory rewards; the same surface serves road caches.

- The description line, then each reward with the item's own title and stack size ("Candle of
  Hope", "Minor Gilded Mind") - the full item tooltip as buffer lines - then Take All, Leave
  Items, and the utility buttons (Hero Sheet, Inventory).
- Enter takes an item through the game's own transfer (invalid-click audio when the player
  inventory is full); the list rebuilds as items leave, re-homing focus.
- Escape runs the game's own close flow: with rewards remaining it opens the game's
  leave-items confirmation dialog, which the dialog screen reads ("You will leave items
  behind. Still press onwards?").

**Known gaps:** Take All's per-item toast stream is unspoken; the utility buttons read via
their tooltips only.

## 7.4 Combat Intros (REALTIME_CINEMATIC mode) - NOT STARTED

The `combat_intro` scene mode (boss intros). Unaudited; no screen matches it, so the keyboard
releases.

---

# Phase 8: The Inn (INN mode)

## 8.1 Travelogue (`InnResultsScreen`) - WORKS

Live-verified 2026-07-24: arrival restore, full arrow walk. The inn-arrival run recap
(`SubScreenBiomeResultsBhv`; the hub's Travelogue button reopens the same surface).

- Reads like a modal: one focusable text row per run-log entry (the game's own rendered
  lines - "The resolute Companions reached The Torch & Crown", "2 Candles gained for reaching
  the Inn!"), then the Loathing meter's tooltip as a readout ("The Loathing Abates,
  Prologue"), then Continue (arrival only; the game hides it on a reopened travelogue).
- Continue activates through the button's own submit; Escape runs the screen's continue flow
  when a continue button stands, else the game's own close.

**Known gaps:** the innkeeper portrait button (unlabeled, flavor) is not modeled; a reopened
travelogue (from the hub) is unexercised live.

## 8.2 Inn Hub (`InnScreen`) - WORKS

Live-verified 2026-07-24 at the prologue inn: name, hero row, stations, full inventory walk,
item tooltip buffers. Named by the inn's own title (`InnBhv.GetInnInstance().Name` already
holds the localized title; authored "inn" fallback).

Layout, top to bottom:

- The regions-to-mountain readout.
- The **hero rest strip** as a horizontal row (`RestHeroElement` over each `RestItemSlotBhv`):
  name with HP and stress from the live actor, the slot's status tooltip as buffer lines,
  Enter through the slot's own submit - which in Kingdoms is the game's own path INTO the
  Select Replacement Hero screen (8.9).
- The **stationed-heroes row** (Kingdoms: the portrait strip by the inn title, from the same
  `InnStationedActorBhv` widgets sighted players see - each reads its class-name tooltip,
  Enter opens that hero's sheet the way the game's right-click does; the pool is empty at
  expedition inns so the row vanishes from the walk).
- The station buttons (captions from their tooltips; the prologue inn genuinely offers only
  Travelogue and End Expedition - the bar rebuilds per inn and later inns add the shops).
- The inventory panel: the filter, "slots 5 / 20", and wallet rows ("Relics, 40") as readouts,
  the sort button, one element per carried item (`InventoryItemElement`, shared with the loot
  screen: the item's own title and stack - "Candle of Hope, 3" - full tooltip in the buffer),
  and the free capacity collapsed to one live line ("15 empty slots").
- Escape opens the pause menu.

Transient text (`InnEvents`, drained by the hub's pump like the combat log, queued so bursts
stack in order): bark bubbles at the inn - rest-item reactions, use-limit refusals, act-out
quips - through the same bark-spawner patch as the road (`BarkEvents` routes by mode);
rest-item refusal pop texts as the hero's name plus the same string the game floats (the
blocking quirk's name, the condition-blocked sentence, or the blocking relationship's name);
and floating affinity changes as the combat tick's line ("Dismas and Audrey, affinity +1").
Plain successful item use has no game text - the bars animate and the slot reads the new
values on demand.

### 8.2.1 Sort and Rebuilds - WORKS

Sort speaks "sorted by type" on press (the game has exactly one sort - item type, then name;
no modes). A sort re-populates the pooled slot widgets with all-new instances, so the item
list rebuild is keyed to an instance-id signature, not a count - and the inventory's frame
elements (filter, count, wallet, Sort itself) sit outside the rebuilt container so focus
survives the press. Verified live: two consecutive sorts, items walkable immediately after
each. **Item-list rebuilds re-home focus** over the widget the cursor sat on (a placement, a
sale, a restock rebuilds our elements; the cursor no longer falls to the top of the screen),
silently - the action's own feedback is the only speech.

### 8.2.2 Filter Tabs - WORKS

The filter reads as a tab ("All Items, tab"): Left/Right apply the game's own tab buttons
through `InventoryUiBhv.ApplyFilter` - the mouse path; the tabs themselves are icon-only and
invisible to a text sweep, and the game's own controller cycling (PrevTab/NextTab) is gated on
EventSystem focus we deliberately never grant. Hidden (HideIfEmpty) tabs drop out of the live
list. Verified live: cycling all five tabs both ways with clamped ends, the item list
re-filtering per tab (Trinkets showed exactly the two trinkets), and the walk restored under
All Items.

### 8.2.3 Discard and Sell (Shift+Enter) - WORKS

**Shift+Enter discards** the focused bag item (the game's shift-click; the whole stack,
instantly - the game confirms nothing except its own last-trophy safeguard). The element
advertises the action only where the game allows it (`m_canDiscard`, player bag slots);
anywhere else Shift+Enter answers "unavailable". Player-verified live. The press speaks its
outcome from the model - "discarded X", or **"sold X"** when a seller is open (the game's own
handler sells ONE item per press in that state instead; the wording follows the same
`GetIsSellingActive && HasSellCost` branch the handler takes). Silence means the game's
confirmation dialog took over, and it announces itself. Sell wording deployed but unverified
live (no seller at the prologue inn; expedition selling is gated on the hoarder option).

### 8.2.4 Grab and Place (Space) - WORKS

**Space grabs and places inventory stacks** (`ItemGrab`) - the keyboard face of the game's
item drag and drop, same key and feedback as the crossroads hero grab. Live-verified
2026-07-24 (dev driver + player keys): whole-stack move, split, accumulate, split-until-empty
ending the grab, merge, swap both directions, both cancels.

- Space on a stack speaks "grabbed X", Space on the landing target places the whole stack, and
  **while a grab is held Shift+Space places a single item off it** (the game's split-stack
  drag), keeping the grab held so repeated presses keep splitting until the stack runs out
  (then the grab ends with the landing line; Shift+Space never initiates - unarmed it answers
  "unavailable").
- Targets: another stack (same item combines - the merge the mouse gets by dropping; different
  item swaps in place, no free slot needed - the full-inventory exchange) and the **"N empty
  slots" capacity line, meaning this inventory's free space** (a single placed there
  accumulates onto an existing partial stack before opening a new slot, and a fresh stack
  opens the LAST empty slot, so it reads at the bottom of the list right above the capacity
  line the cursor sits on).
- Placement mirrors `InventoryItemBhv.DefaultSwap` at the model level
  (`ItemInventory.SwapItems` / `TakeItemQty`, then `EventInventoryItemSwapped` like the game's
  own drop), with the game's `AcceptsItem` rules honored across inventories; a split onto a
  different-item target answers "cannot place here" rather than inherit DefaultSwap's
  whole-stack-swap fallback.
- Same-slot Space or Escape cancels ("grab cancelled"; Escape only falls through to the pause
  menu when no grab is armed). The landing speaks the placed stack's title and new size from
  the model.

> **Note:** Bag position carries no meaning (no adjacency; the game's own Sort reshuffles), so
> the 3-column visual grid is deliberately flattened to an occupied-only list; empties exist
> as the one capacity line. The game's slot-swap flow (`IsSelectingItemSlot`) accepts any
> same-inventory slot as a target, so a grab-and-place flow needs only one "empty"
> destination, never a specific cell. Grab deliberately excludes loot and store widgets (their
> Enter flows already transfer).

### 8.2.5 Equipping - WORKS

**Equipping rides the game's own slot-select flow, end to end** (live-verified both ways):
Enter on a bag trinket/combat item makes the game itself open the hero sheet in slot-select
mode; Enter on a sheet equip slot runs the game's `Swap()` (equips; a held item swaps back to
the bag), and Enter on an occupied slot auto-transfers it off (unequip). Escape anywhere
cancels the mode and falls back to the inn. Sheet equip slots read the equipped item's title
from the model (current the same frame the swap lands, where the widget text is a frame late)
and activation speaks the landed state back ("Minor Gilded Mind" on equip, "Equip Trinket" on
unequip). Bag-to-bag rearrangement has no logical handler in the game at all (mouse-drag
only), so it is deliberately out of scope; REST items use a select-then-apply-to-hero flow
instead of slot-select (unexercised live).

> **Note (embark nag):** the embark button reads once: the game nests a clickable "Select a
> Route" overlay button inside the disabled Rest/Embark button (a mouse-only nag whose caption
> duplicated the station bar's own Select Route button), so the station sweep skips any
> selectable nested under another selectable - the overlay's caption still reaches the Rest
> element's buffer through the parent's tooltip scope.

> **Note (registration order):** the hub deliberately outranks the generic floor: the inn
> keeps its inventory panel (`screen_inn_player_inventory`, an `InventoryUiBhv` stack entry)
> as the top stack entry while the hub is up, and the floor would otherwise capture just that
> panel and strand the station buttons. Any station sub-screen pushed above it hands the
> surface to its own reader.

**Known gaps:** rest-item application onto heroes (the game's select-then-apply flow; our
elements drive the right handlers but no REST item was owned to verify) and shops' richer inns
are unexercised (only the two-station prologue inn verified as a full hub walk). The grab
flow's cross-inventory half (bag to **inn storage**, where `AcceptsItem`
blocks undiscardables) is unexercised: storage is a Kingdoms feature and only the bag exists
at an expedition inn - it needs no new code (grab is target-generic over
`PlayerInventoryItemBhv` slots) but wants a live pass when a Kingdoms inn is next exercised.
The "new item" glow is unspoken (its model flag is consumed on first render - `Refresh` calls
`SetViewed` - so the live signal would be the background director's loop; deliberately skipped
as cosmetic).

## 8.3 The Provisioner (`StoreScreen`) - WORKS

Live-verified 2026-07-24 at the first Denial inn. Over `InnStoreUiBhv`; the same screen serves
road merchants (6.6) and the hospital's Pharmacy tab (6.5). Named by the inn header's own
station title (which retitles a beat after entry, so the entry announce can speak the inn's
name once - known cosmetic race, shared by all stations). Escape closes through the station's
own `CloseSubscreen`.

- Wallet rows, then the store slots - item title, price, and stock from the model and the
  game's own price text ("Bear Trap, button, relic 6, 2"; a sold-out slot reads the game's
  "Out of Stock!"), full item tooltip in the buffer - then the player's bag (shared elements:
  items, free-capacity line) so **Shift+Enter sells one per press** with the "sold X"
  wording.
- Both lists rebuild on an instance-id signature with focus re-homed (pooled widgets recycle
  on every transaction).
- Enter buys through the game's own validated purchase; a landed buy speaks the slot's new
  state, a failed one the game's insufficient-funds line. Live-verified: full walk, one
  purchase (A Glimmer of Hope, stock and outcome spoken). Sell-one press not yet
  player-verified on this screen (identical wiring to the verified inn-hub discard).

## 8.4 Mastery Trainer (`MasteryScreen`) - WORKS

Live-verified 2026-07-24 at the first Denial inn. Over `InnUpgradeSkillsBhv`.

- The hero header (name + "mastery points N"; Left/Right page the party via the trainer's own
  arrows - one utterance per switch: the adjust feedback carries the full new line, and the
  pooled-button rebuild's re-land dedupes against it via the navigator's recorded
  last-announcement, fixed 2026-08-08 user-caught), one element per skill (the game's own name - the base skill's until mastered, then
  the mastered variant's with its upgraded-skill glyph spoken as words, the same switch the
  sighted row makes; states "mastered" / "selected" / "unavailable"; full skill card in the
  buffer), the path seal, "Change Path" with its cost (caption from its tooltip - the visible
  text is only the cost), and Reset - whose visual is a hold gesture, so the element drives
  the real `OnResetPressed`.
- The buffers split the cards the sighted tooltip pairs (spliced form live-verified
  2026-08-03): a skill's control buffer holds its current card (the mastered card once
  mastered, the same switch the game's tooltip makes), and the mastery buffer holds the
  preview (`SkillCard.UpgradeBufferLines` - the `_u` variant's stat bar via
  `GetUpgradeTopBarString` and its per-target effects), or "no upgrade available" once
  mastered. The trainer's buttons carry the unlock id - the `_u` variant - whatever the
  hero's mastery state, so the element normalizes the id before judging mastery and picking
  the displayed card (fixed 2026-08-08, user-caught: every unmastered trainer skill answered
  "no upgrade available" and read the mastered card as its current one).
- Enter queues a skill through the trainer's own `TrySelectSkillToUnlock` (the mouse holds);
  the rebuild announces the new points. A skill selected for upgrade speaks "mastered", not
  "selected" - the game's own row rule (`IsSkillVisiblyUpgraded`): the points are spent and
  the row wears the full mastered look, with only the Reset button distinguishing pending
  from committed (2026-08-08, user-caught; "selected" remains for a pending kingdom skill
  unlock, which the game does not dress as mastered).
- The Change Path panel is its own screen (`MasteryPathScreen`, live-verified 2026-08-08),
  so opening and closing announce themselves: named by the game's own panel title ("Change
  Hero Paths"), it reads one element per path seal (the game's seal label with its bonus
  candle glyphs; "selected" on the highlighted seal), the "path details" readout, then the
  purchase button ("unavailable" while the selected path is the active one, armed once a
  different path is previewed). The panel stays permanently active with a CanvasGroup riding
  visibility, so the screen matches on `blocksRaycasts`.
- A path seal's Enter previews through the trainer's own `SelectPath` (drives the comparison
  panel and arms the purchase button; nothing commits) and re-announces "selected". Its
  buffer carries that path's own card without needing selection (`GetDescriptionString`, the
  hero-seal tooltip text: name, class path, quote, effects, affected skills), and the hero
  buffer answers with the vitals. The "path details" buffer reads the live comparison panel
  through `PathComparison`: title, flavour, and effects from the panel's own data context
  (bound TMP applies a frame late), then the coverage pips as authored count lines -
  "Skills per rank: 2, 2, 1, 0" / "Skills per enemy rank: 1, 2, 1, 1", ranks 1 to 4
  ascending (the panel draws launch pips rank 4 down to 1). A pip's fill is the rank's
  skill count over the equip limit, so the count decodes exactly.

Live-verified: walk, hero paging wiring, queue ("selected", points drop), Reset (queue
cleared, points restored), the path panel (open/close announcements, seal cards, preview
select, pip lines, purchase-button arming). Unexercised: Apply/commit (the batch confirm),
an actual path purchase, hero paging with a full party. Known quirk: the trainer's entry
announcement can read the inn's name instead of "Mastery Trainer" - the station header
binds a frame late (`InnStations.Title`); the fold-back from the path panel reads it right.

## 8.5 The Wainwright (`WainwrightScreen`) - WORKS

Live-verified 2026-07-24 at the first Denial inn. Over `StageCoachConfigUiBhv`; the same class
covers the read-only road sheet on Z (5.6).

- The coach's name from the model (renaming is unmodeled), wallet, the game's own composed
  stat lines ("Cargo Slots: 20", "Armor: 2/2", damage explanations in the buffer), a "repair,
  baubles 8" button per stat (the game's own transaction; `cost_` currency glyphs speak - the
  faction glyph as the authored "baubles", no game string spells it), the livery cycler, and
  the upgrade slots as equip slots (altar-locked ones carry their lock text).
- The livery cycler (2026-08-12) reads as the game's "Stagecoach Livery" title with the
  applied skin's name as its value ("Stagecoach Livery, Ironclad, button"; it read as a bare
  unlabeled button before), and Enter's cycle re-reads the landing so the new skin is spoken.
  Names are the game's `stagecoach_skin_<id>` strings; the base and kingdom-faction liveries
  have none anywhere in the game, so the id's own words stand in ("beastmen", "coven"), the
  base one as the game's "Default". The game greys the button below two unlocked liveries,
  read as unavailable.

Live-verified: full walk. Unexercised: a repair press (stats were full), equip/unequip on this
sheet.

## 8.6 Select Route (`RouteSelectScreen`) - WORKS

Over `SubScreenBiomeChoiceBhv`.

- One element per offered route - the destination's own name, "selected" state, Enter marking
  the choice through the game's own submit - or "empty" when the inn offers none.
- The buffer carries the offer in the slot's visible order: the modifier name (full from the
  model, where the label may be ellipsized) with its effect tooltip, the goal name with the
  goal tooltip, then the game's own "Reward:" header and the rewards - mastery points as the
  game's "+2 Mastery" run-log wording with the count from the model (the slot's bound label
  shows only "+2" and a glyph), an item through the reward icon's tooltip (title, type with
  stack count, description). Tooltips outside that composition (the mountain's equip-trophy
  prompt) still read, after it.

Live-verified 2026-08-10 at a candle-reward inn: both routes' full buffers (the modifier and
goal names and the reward header/mastery count did not read before). The mastery-point branch
was exercised by swapping a forged hero-points reward into the in-memory model (restored
after). The empty state was live-verified earlier at a Denial inn that rolled zero choices
(`GetCanEmbark` false there - if departure refuses, that state is why). Unexercised: the
mountain slot's equip-trophy prompt, the heropoints description tooltip on a real mastery
offer.

## 8.7 Relationships Matrix (`RelationshipMatrixScreen`, Kingdoms) - WORKS

Live-verified 2026-07-26 at the Alpenglow inn. Over `SubScreenRelationshipMatrixBhv`.

- The anchor hero first (name and class - the panel the portrait grid pivots around), then one
  element per other roster hero - name and class from the actor model (the tiles' identity is
  portrait-only, no text), then the anchor's relationship as the tile draws it: the band word
  with the affinity meter ("Neutral, 11/20"), plus the formed relationship's remaining days
  while the tile shows them.
- Enter re-anchors the matrix through the tile's own click; the pooled rebuild re-lands on the
  anchor readout, which names the new anchor.

Verified: walk, a re-anchor (readout renamed, tiles pivoted), Escape to the hub, reopen from
the hub's button. Unexercised: a formed relationship's name and countdown (all pairs were
Neutral). The game's right-click shortcut to the partner's character sheet is unmodeled.

## 8.8 Inn Upgrades (`InnUpgradesScreen`, Kingdoms) - WORKS

Live-verified 2026-07-26 at Alpenglow. Over `SubScreenInnUpgradeBhv`.

- A tabbed screen: the category tab first, Left/Right stepping the tab group's OWN association
  order via its click handler (the prefab's page order differs from the panel's
  handler-method order, and the group's m_activeIndex is not authoritative - its click loop
  overwrites it per association - so the current tab is read off the visibly active page; both
  directions verified symmetric), the game's materials line, then the active category's tree
  as `InnUpgradeNodeElement`s (shared with the map inn panel's tree, 10.2).
- A node reads: owned ahead of the name, or the name then the game's composed cost, then WHY
  it cannot be bought,
  mirroring what the tree shows - "needs X, Y" from the prerequisite wiring (verified: single
  and multi-prereq chains), the game's own Insufficient Funds line for a red cost (verified),
  and the level-restriction banner text for out-of-tier rows (code path present; this inn had
  no level gate to observe). Description in the buffer, plus the category's verbose flavour on
  the ultimate node (verified: the Barracks "extensive training regimen" text).
- Enter purchases through the node's own gated Unlock. Escape closes through the station's
  sub-screen flow. Unexercised: an actual purchase (commits the save).

## 8.9 Select Replacement Hero (`InnReplacementScreen`, Kingdoms) - BUILT

Deployed, needs live pass. Previously read by the generic floor: class names only - no hero
names, no at-this-inn markers. The kingdoms hero swap screen
(`InnReplacementScreenWidgetBhv`, pushed by Enter on a rest slot).

- The Stationed Hero Effects readout first (its tooltip - what a hero stationed here gains -
  as buffer lines), then one row per candidate (`InnReplacementRowElement`): the hero's
  **name then class** from the row's own bindings ("Paracelsus, Plague Doctor"; the random row
  reads its "Random Hero" label alone), **"at this inn"** as the value exactly where the game
  shows its inn marker on the row (`m_innActorObj`, driven by the current map cell's
  `ActorGuids`), and the row's add/station tooltip as buffer lines - "Add hero to party", or
  "Station hero" on the last row, which is the current party hero and whose Enter REMOVES
  them to the inn (the buffer is the distinguisher, matching the sighted hover tooltip).
- Enter is the row's own submit; Escape closes through the screen's own teardown.

## 8.10 End Expedition - WORKS

The inn's End Expedition button leads to the results surfaces; see 9.1 (live-verified there).

---

# Phase 9: Run End and Results (RESULTS mode)

## 9.1 Results Surfaces (on the generic floor) - WORKS

Live-verified 2026-07-24 on the inn's End Expedition screen ("Every League, a Lesson.").

- The score-row prefab these screens share (`GameOverScoreLabelBhv`) reads as a readout
  composed like the sighted row - the game's reason label plus its number ("Candles Found: 3";
  a 0 is what the visual cross mark means) - with the row's explanation tooltip in the buffer,
  followed by the run total ("total 5"), which the visual panel shows only as a bare number
  beside a candle icon (no game caption string exists; the total reads the game-over flow's
  pre-composed line as-is when the binding holds one). The same code serves the game-over and
  Kingdoms results screens (deployed, unverified there). Collect Hope reads as the ordinary
  button it is.
- **Rebuilds are silent while the surface grows**: the results screens animate their score
  rows in one at a time, and each arrival re-populates the tree. Elements are keyed to their
  live widget and reused across rebuilds (the rebuild check is an instance-id signature, not a
  count), so the focused element survives and nothing re-announces - the game-over screen used
  to queue "Continue, button" once per arriving row (observed live 2026-07-24). Focus falls
  and announces only when the focused widget itself is gone.

**Known gaps:** the screen name is read at entry, before a results screen's late-bound title
has text - the game-over screen announced itself as "Continue" (its first readable label at
that instant) rather than its title.

## 9.2 The Mountain and Final Confession - NOT STARTED

The end-of-run mountain flow rides covered surfaces (driving, combat, results, cinematics) but
has had no dedicated audit pass; boss-specific presentation is unaudited.

---

# Phase 10: Kingdoms

Kingdoms entry, save select, and the creation wizard are in 1.5. Kingdoms inns add the
stationed-heroes row (8.2), Select Replacement Hero (8.9), the Relationships matrix (8.7), and
Inn Upgrades (8.8). The gang escalation tooltip in siege combat is an open gap (7.1.5).

## 10.1 Kingdom Overworld Map (`KingdomMapScreen`) - WORKS

Live-verified 2026-07-26 on the Drakia save, day 1. Actions that commit the save (travel, pass
day, engage siege, hero transfer, boss travel) are wired through the game's own handlers but
deliberately unexercised. The map is NOT a stack screen: opening it pops the inn's inventory
entry and shows its own presentation layer. `KingdomMapScreen` matches `KingdomBhv.IsMapOpen()`
with no SCREEN stack top (any pushed screen - cell panel, hero sheet, storage - covers the map
and reads instead), registered above `InnScreen`. Escape = `KingdomPresentationBhv.CloseMap()`,
which the game itself refuses during day-turn cinematics (spoken "unavailable").

- **Grid cursor** (first Tab stop, Panel root - Tab crosses cursor / header / sieges / heroes,
  wrapping both directions): arrows step the 9x9 cell grid, Home returns to the stagecoach,
  landings mirror into the game's own selection (camera pan, highlight, travel-arrow preview -
  all presentational) when the cell is selectable.
- **A cell reads view-first, so hidden content cannot leak by construction**: the pre-reveal
  boss cell and its 8-neighbour ring deactivate their view objects and read as "empty";
  treasure reads from the cell's own bindings (the game hides it on the occupied cell); siege
  strength speaks only the 3-band bucket the icon shows; a boss-subtype biome is nameless on
  screen and stays so.
- Cell line: name, stagecoach here / travel scheduled / reachable (user-input states only),
  stationed hero names, siege (band + days), treasure (+ duration), biome markers
  (cursed/quest, kill contract title, reward offered, upgraded). Buffer: the cell's own
  tooltips ("The Stagecoach is here", hero classes), fast-travel state, hero name+class lines,
  grid position.
- Enter = the game's `EventKingdomActivateMapCell` (gated on `IsInUserInputState`): inn/camp
  opens the inn panel, biome the biome panel, boss travels or shows the game's blocked dialog.
- **Header** readouts: day, PASS DAY button, current-event button (opens the event panel),
  escalation level (widget tooltip as buffer), timeline (last-day line; buffer = the marked
  days only: escalation surges, quest steps, final day).
- **PASS DAY** commits in the game on a one-second pointer hold (`UIPointerDownBhv` into
  `SetPassHeld`; the Button's onClick is unwired, so a submit press did nothing). Enter runs
  the game's own `OnPassDay` behind the hold's gates (`CanPassDay`, alt view,
  `WAIT_ON_PLAYER`) with the hold's confirm sound; a refused press answers "unavailable"
  (day advance player-verified live 2026-08-01).
- **Sieges**: one element per active siege - inn name + days; Enter jumps the cursor to the
  cell, handing focus straight back to the grid cursor at the jumped-to cell.
- **Heroes**: party + reserve rows (name, class, travel-scheduled state; Enter on a reserve
  hero enters the game's own hero-travel mode, gated to WAIT_ON_PLAYER).

> **Note:** Panel entry announces read the cell's name from the game's viewed-cell query (set
> before the push), so they are correct from frame one; the populate re-land a beat later
> stays silent when its line matches the entry landing verbatim (the router's
> AnnounceCurrentIfChanged), so only a genuinely different line (the inn panel's garrison
> arriving) speaks twice.

**Known gaps:** sidebar cursed-regions counter unread; travel-path preview for hero transfers
is visual only.

## 10.2 Inn/Camp Cell Panel (`KingdomInnPanelScreen`) - WORKS

`ScreenKingdomMapInnPanel`, named by the inn from the model.

- Garrison (hero name+class, militia class slots, travel/immobile status tooltips in the
  buffer), defenseless label, travel / fast travel / engage siege / storage buttons as the
  game shows them, the five upgrade tabs (loc-named: Barracks etc.), treasure rewards, and the
  close button.
- Enter on a tab opens the **upgrade tree** view: one element per node - owned leading, the
  name, the game's composed cost ("materials 5"), unavailable when locked/unaffordable, description in
  the buffer; Enter purchases via the node's own gated `Unlock()` (the sighted gesture is a
  hold). Escape folds the tree first, then closes (the panel's own two-stage back).
- **Garrison reordering** (live-verified: swap spoken as the new order, model confirmed): the
  grab key picks up a stationed hero and places it on another garrison slot - the two widgets
  swap slots and the order commits through `SetActorOrder` via the panel's own
  `GetActorOrder`, the same call its drag release runs. Militia slots refuse the grab; Escape
  drops a held hero first.

## 10.3 Biome Cell Panel (`KingdomBiomePanelScreen`) - WORKS

- The enemy roster under the game's own Enemies header (model-composed so it is complete on
  the entry frame; the biome's name is the screen name), expedition rewards composed from
  the item model like an inventory slot - title then stack size, the icon's own text being
  only the count badge (`RewardItems`, shared with the inn and event panels; live-verified
  2026-08-01) - upgrades/modifier text and kill contract gated on the model (the labels keep
  template text when unbound), reward tooltips in buffers, and the close button
  (prefab-wired, found by a text-bearing button sweep).

## 10.4 Event Panel (`KingdomEventPanelScreen`) - BUILT

`ScreenKingdomMapEventPanel`, the day/event notification: day + title + effect + flavour as
one element, rewards, close button; Escape via TryCloseScreen (the game swallows it during the
slow day-intro). Model-built, NOT yet seen live (needs a day to pass).

## 10.5 Inn Storage (`InnStorageScreen`) - WORKS

Live-verified: stored stack read, grab-and-place within storage, model-change rebuild. Over
the `InnStorageBhv` stack entry.

- The inn name, the storage list (occupied stacks + free capacity line, shared
  `InventoryItemElement` machinery), the frame's Inventory button, and - when the bag screen
  is open beneath (the inn-hub path) - the full shared bag panel, one grab spanning both
  inventories. From the map path the game shows storage alone; the Inventory button flips
  views.

**Known gaps:** cross-inventory grab (bag to storage with both lists open) is wired through
the verified shared flow but that both-open state was not reached live.

---

# Phase 11: Cross-Cutting Notes and Gaps

## 11.1 Testing Rule Learned the Hard Way

The dev server's `/input` drives the navigator's logical handlers directly and proves screen
logic only - it does NOT exercise the physical keyboard path (`KeyboardBinding` polling). A
broken key reader shipped while every scripted test passed. Any change touching input must be
verified with device-level events (`InputSystem.QueueStateEvent` via `/eval`) or real key
presses.

## 11.2 Known Cross-Cutting Gaps

- **Mode surfaces without a screen release silently.** The generic floor (2.9) gives every
  pushed SCREEN stack entry a reading, but a mode surface with no stack entry (as the altar,
  embark, and the hero story intro once were; combat intros still are - 7.4) releases the
  keyboard with no announcement - dead air until a dedicated screen is written.
- DataContext-bound text applies a frame late; anything read at commit time must come from loc
  keys or the model, not the TMP.
- Station/panel titles that bind a beat after entry can cost the entry announce the right name
  (the inn-station retitle race in 8.3, the results title race in 9.1).

---

# Phase 12: Summary of All Surfaces

## 12.1 Mode Surfaces (one per `GameModeType`)

- MAIN_MENU - main menu (1.2) WORKS; kingdoms scene overlay (1.5) WORKS
- HERO_SELECT - crossroads (4.1) WORKS
- EMBARK - embark staging (4.4) PARTIAL
- DRIVING - driving HUD (5.1) WORKS + audio layer (5.2) PARTIAL
- COMBAT - combat (7.1) WORKS
- INN - inn hub (8.2) WORKS
- ALTAR_OF_HOPE - altar hub + panels (Phase 3) WORKS
- HERO_STORY_INTRO - chapter card (6.4) BUILT
- RESULTS - results surfaces via the floor (9.1) WORKS
- CINEMATIC - stands down by design (1.4) N/A
- REALTIME_CINEMATIC - combat intros (7.4) NOT STARTED
- LOADING - N/A

## 12.2 Stack Screens with Dedicated Readers

Settings (2.1), pause (2.2), confirmation dialogs (2.3), generic modal (2.4), token glossary
(2.5), tutorial archive (2.6), patch notes (1.6), hero sheet (4.2), inventory (5.7), road map
(5.3), fork menu (5.4), confession select (5.5), stagecoach sheet (5.6/8.5), node prompt
(6.1), road stories (6.3), field hospital (6.5), store (6.6/8.3), loot (7.3), inspector (7.2),
travelogue (8.1), mastery trainer (8.4), select route (8.6), relationships matrix (8.7), inn
upgrades (8.8), replacement hero (8.9), kingdom cell panels and storage (10.2-10.5), the altar
sub-screens and reveals (Phase 3).

## 12.3 Surfaces on the Generic Floor by Design

Any unmodeled pushed SCREEN stack entry (2.9), results screens (9.1), the hero sheet's
Conditions/Story/Cosmetics tabs (4.2.3), the kingdoms wizard's mods step (1.5).

## 12.4 Audio Layers

Road cues - the pickup ping, turning, turn-end, road edge (5.2; only what can be steered
at or steered with keeps a sound); combat target-validity beeps (7.1.2).

## 12.5 Uncovered Surfaces (consolidated)

- Mods manager panel details (1.8: Enable/Disable All toggles, mod rows, Browse Mods target)
- Key rebinding flow (2.1.2)
- Credits (2.8)
- Combat intros, REALTIME_CINEMATIC (7.4)
- The Mountain's boss-specific presentation (9.2)
- Kingdoms siege gang-escalation tooltip (7.1.5 gap)
- DEBUG-tab filter field; sliders' display values (2.1 gaps)

---

*End of Document*
