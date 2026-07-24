# DD2A11y screen audit

Per-screen accessibility status. Update this in the same change that adds or fixes a screen.
Statuses: **works** (live-verified), **built** (code exists, not yet live-verified), **planned**,
**not started**.

## Conventions every screen shares

- Arrows navigate the mod's own focus; Enter activates; Escape backs out; Home/End jump.
- Focus lines are terse (label, role, value). Tooltips and detail are buffer lines:
  Ctrl+Up/Down step lines, Ctrl+Left/Right switch buffers.
- Modals read their text first, then each choice, all on Up/Down.
- Tabbed screens put the tab selector first: Left/Right switch tabs, Down enters the tab's
  items, and the screen remembers its tab across close/reopen.

## Screens

### Main menu (`MainMenuScreen`)
Status: **works** (live-verified 2026-07-23)
- Disclaimer text + continue control (drives `OnMainMenuPress`); then the game's own ordered
  selectable list. Icon-only footer buttons (Exit Game, Patch Notes, Cinematics, Mailing List)
  read via their tooltip; the Confessions/Kingdoms tooltips land in the buffer.
- Escape opens settings (the game's own Escape behavior at the title).
- The Confessions submenu is a container swap the count-rebuild picks up.
- Known gaps: a control focused mid-open-animation can briefly read as bare "button" before the
  label-arrival re-announce lands; the profile button reads the profile name (its "Change
  Profile" caption is a buffer line); list order is the game's serialized order, not visual.

### Settings (`OptionsScreen`)
Status: **works** (live-verified 2026-07-23)
- Tab selector + active tab's rows in one vertical flow; rows: `OptionsItemBhv`
  toggles/sliders (labels/tooltips from loc keys), bespoke widgets (language dropdown verified)
  generically. Toggle round-trip and value re-announce verified.
- Remembered tab verified across close/reopen, including the corrective re-announce after the
  game's open animation stomps the tab back to the first one.
- Escape closes in one press from both the title menu and pause (the game's own Escape is
  two-stage on mouse+keyboard; we fold it).
- Known gaps: keybind rows read as bare buttons (no rebind flow); DEBUG-tab filter field
  unhandled; sliders speak normalized percent, not the game's display value.

### Pause menu (`PauseScreen`)
Status: **works** (live-verified 2026-07-23)
- Buttons from the game's own navigation order (Return, Glossary, Options, Tutorials, Patch
  Notes, Feedback, Exit); decorative selectables with no text source (profile badge) skipped.
- Escape = the menu's own Return. Options-from-pause round trip verified.

### Confirmation dialogs (`ConfirmationScreen`)
Status: **works** (live-verified 2026-07-23 with the exit-game dialog)
- Title + body first, then confirm/decline; Escape declines; underlying screen re-announces
  with focus restored to the button that opened the dialog.

### Generic modal (`UiModalScreen`)
Status: **built** - no UiModal appeared during live testing yet.

### Crossroads (`CrossroadsScreen`, HERO_SELECT mode)
Status: **works** (live-verified 2026-07-23)
- Party ranks (the game's "roster slots", Rank1-4) then the hero pool as horizontal strips,
  then embark controls. Hero labels are the game's own class-name loc keys; locked heroes say
  "unavailable" with their flavor/unlock text as buffer lines; drafted pool heroes read
  "in party".
- Enter = the game's own two-step (select a hero, then Enter on a rank places them).
  **Space** = grab-and-place through the game's drop logic (specific rank, rank swap, back to
  pool), with grabbed/cancelled/cannot-place feedback. **I** = the hero sheet (the mouse
  right-click equivalent), read by the generic floor screen; Escape closes it.
- Known gaps: the hero sheet is floor-level only (skill buttons read as "1"/"2", no skill
  names/tooltips - needs a dedicated screen); path-select and party-loadout sub-panels
  (`HERO_SELECT_PATH_SELECT` / `HERO_SELECT_PARTY_LOADOUT`) not modeled; stagecoach config not
  started; relationship buttons and the embark press itself not yet exercised.

### Generic floor (`GenericScreen`)
Status: **works** (live-verified 2026-07-23 on the hero sheet)
- Any pushed SCREEN stack entry with no dedicated reader gets a generic sweep of its labeled
  selectables, so no surface is dead air. Registered above the mode screens (a pushed screen
  covers the scene) and below the dedicated stack screens. Driving HUD widgets (minimap,
  goals - non-SCREEN stack entries) are excluded so free driving is never captured.

### Everything else
Status: **not started** (floor-level reading only) - driving/map, combat, inn, loot, node
panels, glossary, tutorials, academy/altar, profile select, save management, kingdoms.

## Testing rule learned the hard way

The dev server's `/input` drives the navigator's logical handlers directly and proves screen
logic only - it does NOT exercise the physical keyboard path (`KeyboardBinding` polling). A
broken key reader shipped while every scripted test passed. Any change touching input must be
verified with device-level events (`InputSystem.QueueStateEvent` via `/eval`) or real key
presses.

## Known cross-cutting gaps

- **Unsupported screens release silently.** Opening a screen the mod does not model (glossary,
  the road's node-arrival panel) releases the keyboard with no announcement - dead air. A
  generic fallback screen that reads any `Selectable`s on the topmost stack screen would give
  every surface a floor.
- DataContext-bound text applies a frame late; anything read at commit time must come from loc
  keys or the model, not the TMP.
