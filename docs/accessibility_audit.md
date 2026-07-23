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
Status: **built** - NOT live-verified. The test profile's save sits mid-run (Continue loads
into DRIVING) and no New Confession path was available without touching the player's run, so
the screen never resolved live. Party strip / roster strip / embark controls modeled; hero
detail comes from slot tooltips into the buffer. Verify on the next fresh expedition, then
model the path-select and party-loadout sub-panels (`HERO_SELECT_PATH_SELECT` /
`HERO_SELECT_PARTY_LOADOUT`) and the stagecoach screen.

### Everything else
Status: **not started** - driving/map, combat, inn, loot, node panels, glossary, tutorials,
academy/altar, profile select, save management, kingdoms.

## Known cross-cutting gaps

- **Unsupported screens release silently.** Opening a screen the mod does not model (glossary,
  the road's node-arrival panel) releases the keyboard with no announcement - dead air. A
  generic fallback screen that reads any `Selectable`s on the topmost stack screen would give
  every surface a floor.
- DataContext-bound text applies a frame late; anything read at commit time must come from loc
  keys or the model, not the TMP.
