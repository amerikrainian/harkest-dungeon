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
  then the actions strip: the party's name when the composition has one, **Embark** (appears
  once all four ranks are filled - drives the game's own `ConfirmRosterSelection`, including
  its unequipped-skills confirmation dialog), and **Random Party**. Hero labels are the game's
  own class-name loc keys; locked heroes say "unavailable" with their flavor/unlock text as
  buffer lines; drafted pool heroes read "in party". Every hero slot's buffer ends with the
  class blurb the sighted panel shows (`actor_verbose_description_*` / `actor_descriptors_*`:
  the flavor line and the "+ Front Rank + Guard..." descriptor list); the same lines lead the
  hero sheet header's buffer.
- Enter = the game's own two-step (select a hero, then Enter on a rank places them).
  **Space** = grab-and-place through the game's drop logic (specific rank, rank swap, back to
  pool), with grabbed/cancelled/cannot-place feedback. **C** = the hero sheet (the mouse
  right-click equivalent, matching the game's own "Hero Sheet (C)" hint), read by its
  dedicated screen; Escape closes it.
- Known gaps: the Embark element is live-verified up to (not including) the press - pressing
  it starts the run, which leads into unmodeled screens. The path-select and party-loadout
  canvas overlays are not modeled, so their opener buttons (the "Change Path" seal,
  "Party Loadouts") are deliberately NOT surfaced - offering a control that opens an
  unreadable overlay is a trap; surface them together with their panels. Stagecoach config
  not started; hero rename/reroll on the canvas not surfaced; the party's aggregate
  Rank/Target pips are not read (each skill's exact ranks are in the hero sheet).

### Hero sheet (`CharacterSheetScreen`)
Status: **works** (live-verified 2026-07-23 from the crossroads)
- Layout: hero header (name, then class and path; **Left/Right page through the heroes**, the
  path description is buffer lines), the sheet's tab selector, then the active tab's content.
- Skills tab (the sheet's main view) reads from the game model: HP/stress/speed (each with its
  tooltip breakdown as buffer lines), the nine resistances (displayed value; base/modifier
  breakdown in the buffer), quirks (name; description in the buffer, re-read live so rerolls
  never go stale), each combat skill as a toggle - Enter equips/unequips through the game's own
  button - with the full skill card as buffer lines (Rank/Target lines with multi-hit "+"
  joins, DMG/CRIT/cooldown, per-target effects, melee/ranged), then the combat item and
  trinket slots.
- Inline effect glyphs in game text (tokens, dots, heal/buff/debuff and stat icons) are spoken
  as words: token and dot names resolve through the game's own `token_name_*` / `dot_name_*`
  strings; the icons with no name string anywhere in the game (heal, buff, debuff, stress,
  disease, speed, HP) carry authored words; any other icon speaks its humanized sprite name
  rather than silently dropping ("-2 speed" on a trinket, not a bare "-2"). Known-decorative
  glyphs (the hero-seal mark) are the only ones dropped. Applies pipeline-wide - every buffer
  and announcement benefits.
- Other tabs (Relationships, Conditions, Story, Cosmetics) read as a generic sweep of the tab
  panel's labeled selectables, with the panel's own text - or "empty" - as the floor; verified
  live: Relationships "empty" pre-run, Conditions "Memories", Story its unlock hint, Cosmetics
  its palette buttons.
- Verified: equip toggle round-trip (on/off/on), hero switching rebuilds all content, tab
  switching (both our selector and the game keeping the tab across hero switches), Escape
  closes through `HideCharacterSheet` with the crossroads re-announcing, physical **I** key
  entry from a hero slot.
- Known gaps: hero rename (the name input field and edit button) is not modeled; the cosmetics
  tab is floor-level (palette slots read as bare numbers); an equipped trinket's slot label is
  its "Equip Trinket" caption with the item detail in the buffer (not yet exercised with an
  equipped trinket); the game's own tab hotkeys and tooltip-view mode are not used.

### Generic floor (`GenericScreen`)
Status: **works** (live-verified 2026-07-23, originally on the hero sheet before its dedicated
screen existed)
- Any pushed SCREEN stack entry with no dedicated reader gets a generic sweep of its labeled
  selectables, so no surface is dead air. Registered above the mode screens (a pushed screen
  covers the scene) and below the dedicated stack screens. Driving HUD widgets (minimap,
  goals - non-SCREEN stack entries) are excluded so free driving is never captured.

### Combat (`CombatScreen`, COMBAT mode)
Status: **works** (live-verified 2026-07-23: full hero turn - skill pick, target pick,
execution, kill, turn handoff - plus pause round trip)
- Layout, top to bottom: the battle header ("round 1, Audrey"; torch value, round detail, and
  retreat odds as buffer lines), the enemy strip, the party strip (both rank-ordered; labels
  are name + Rank + HP read live; a monster's name is its data id's loc string, the same
  source as the game's turn-order tooltips), the skills row (horizontal, with the game's own
  "Uses: N" limit text), then the commands row (Move, Pass, and Retreat when the game offers
  it).
- The turn: Enter on a skill runs the game's own pick handler and announces "select target";
  every combatant then reads its validity for the chosen skill (the same
  `GetIsValidSkillTarget` check the game runs on a click); Enter on one sends the game's own
  actor-pick event to execute. Escape cancels target-select first, else opens the pause menu.
- Combatant buffers: HP, stress (heroes), then one line per token and per dot from the game's
  own tooltip composers. Skill buffers: the full skill card (shared `SkillCard` composer with
  the hero sheet).
- **Battle events** are announced as they happen (queued, so narration stacks in order) and
  kept in the **combat buffer** (Ctrl+Left/Right; follows the latest line; empties when the
  battle ends): damage taken ("Lost Soul took 4 damage"; the number dropped when it is 1),
  deaths, death's-door falls, what enemies do ("Lost Soul used Chomp on Paracelsus") - never
  the player's own skill picks - and turn lines ("round 2, Audrey", spoken exactly once via
  the router's announce chokepoint even as the rebuild re-homes focus to the header).
  Verified live: hero attack damage, enemy skill + damage narration, single turn lines.
- Known gaps: crits, heals, stress damage, resists, and token applications are not yet
  event lines (the CombatEvents handlers are the plug-in point); Move is untested against
  position targeting; Pass briefly announces "select target" before auto-resolving; the
  retreat element only (dis)appears on turn-boundary rebuilds; stealth/corpse/summon edge
  cases unexercised; the academic view and token-glossary overlays not modeled; battle-end
  cleanup fires "Corpse died" lines (real death events for the corpse entities - noise at
  the end of a won fight, informative mid-fight).

### Victory / loot (`LootScreen`)
Status: **works** (live-verified 2026-07-23: item buffer, single take, leave-items dialog,
last-item auto-close)
- The loot screen (a battle's Victory rewards; the same surface serves road caches): the
  description line, then each reward with the item's own title and stack size ("Candle of
  Hope", "Minor Gilded Mind") - the full item tooltip as buffer lines - then Take All, Leave
  Items, and the utility buttons (Hero Sheet, Inventory). Enter takes an item through the
  game's own transfer (invalid-click audio when the player inventory is full); the list
  rebuilds as items leave, re-homing focus.
- Escape runs the game's own close flow: with rewards remaining it opens the game's
  leave-items confirmation dialog, which the dialog screen reads ("You will leave items
  behind. Still press onwards?").
- Known gaps: Take All's per-item toast stream is unspoken; the utility buttons read via
  their tooltips only.

### Driving (`RoadSense` + `RouteChoiceScreen`, DRIVING mode)
Status: **built**; cues live-verified by ear (pickup ping confirmed audible), fork menu not
yet reached in play
- Free driving stays UNCAPTURED - the game keeps WASD (W rolls/cruises, S brakes, A/D
  steer, M/I/G/Z/C its own screens). The mod adds an audio layer through its own NAudio
  output (independent of FMOD; `assets/audio`, placeholders replace 1:1): the nearest
  uncollected pickup pings on a 0.7 s cadence, panned to its bearing and louder as it
  nears; collection plays a blip and speaks the item's own title; road damage plays the
  penalty cue and speaks the combat damage wording; the coach's stop/start each cue; a
  junction's banners coming up cue "fork ahead" (once per junction).
- The fork menu (`RouteChoiceScreen`) opens when the game's own junction wait halts the
  coach unchosen: routes in left-to-right order read "direction, destination" (the game's
  road-indicator titles; "Unknown" unrevealed - the hidden type is never leaked), each
  focus playing the destination's identity tick panned to its side. Buffers: description,
  which heroes prefer the route, banner tooltips. Enter commits via the banner's own
  OnClick (game audio + narration; the coach then drives itself); Escape dismisses that
  junction back to manual steering (steer at a banner holding W, the game's hold-to-fill).
- Known gaps: fork menu unexercised live; zone enter/exit, danger stretches, coach
  damage/break, opt-in prompts, and Loathing cues have assets but no wiring yet; the
  minimap and distance-to-Inn readouts are unread (a status readout key is the natural
  next step); edge-tone lane keeping unused.

### Road stories (`StoryScreen`)
Status: **works** (live-verified 2026-07-23 on "Help Us!"; the commit event is wired but
deliberately not pressed in testing)
- Every road story's choices are heroes; each reads name + HP + stress, with the full
  consequences in the buffer: the hero's bark line, then the sighted Alt panel's own
  preview lines (loc-keyed descriptions with values - "party, Flame 100",
  "party, Supplies"), split party/enemy. Enter fires the game's own selection event (the
  click-and-hold equivalent), honoring its hoverable gate; C inspects the hero. The
  narration itself is the game's voiced narrator, already audible.
- The node-arrival panel (`screen_enter_node_panel`) reads on the generic floor: one
  engage button; Escape declines back to the road.
- Known gaps: choices spawning after screen entry leave focus on the utility buttons until
  the player moves (Home reaches the choices); story RESULT presentation is unread beyond
  the narrator; relationship banners and affinity previews unspoken.

### Everything else
Status: **not started** (floor-level reading only) - map overlay, inn, glossary,
tutorials, academy/altar, profile select, save management, kingdoms.

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
