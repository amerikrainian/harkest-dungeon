# Harkest Dungeon screen audit

Per-screen accessibility status. Update this in the same change that adds or fixes a screen.
Statuses: **works** (live-verified), **built** (code exists, not yet live-verified), **planned**,
**not started**.

## Conventions every screen shares

- **Advertised hotkeys work on captured screens** (live-verified 2026-07-25 on a road
  story): the game captions its screen shortcuts on the buttons themselves ("Map (M)",
  "Inventory (I)", "Hero Sheet (C)"), and the input gate swallows those keys - so M, I,
  and C activate the button carrying that caption in the current tree, through its own
  onClick. C first tries the focused element's inspect action (a hero), then the "(C)"
  button.

- Arrows navigate the mod's own focus; Enter activates; Escape backs out; Home/End jump.
- Focus lines are terse (label, role, value). Tooltips and detail are buffer lines:
  Ctrl+Up/Down step lines, Ctrl+Left/Right switch buffers.
- Modals read their text first, then each choice, all on Up/Down.
- Tabbed screens put the tab selector first: Left/Right switch tabs, Down enters the tab's
  items, and the screen remembers its tab across close/reopen.

## Screens

### Main menu (`MainMenuScreen`)
Status: **works** (live-verified 2026-07-27)
- Disclaimer text + continue control (drives `OnMainMenuPress`); then the buttons in VISUAL
  order (user-caught 2026-07-31: the serialized selectable list groups the footer first, so
  the menu read upside down - Exit Game first, Confessions near the end). The sweep sorts by
  screen position: rows top to bottom, left to right within a row (grouping by a quarter of
  a button's world height - the footer buttons sit 3px apart in Y, one row; the Origin Pack
  side promo rides 25px below the stack's Kingdoms and must not merge into its row). The
  open animation slides buttons in, so a changed order signature rebuilds only after holding
  two frames and elements are reused per button - the settle re-sorts silently, and the
  Confessions submenu swap (which deactivates the main stack a frame before its own buttons
  arrive) re-homes onto "Continue Confession" rather than a mid-swap transient. Verified
  order: Confessions, Kingdoms, Origin Pack, Mods, then Exit Game / Mailing List / Patch
  Notes / Cinematics left to right, the Journal (bottom-right corner) last; submenu:
  Continue Confession, New Confession. Icon-only footer buttons (Exit Game, Patch Notes,
  Cinematics, Mailing List) read via their tooltip; the Confessions/Kingdoms tooltips land
  in the buffer.
- Through the open animation (after the disclaimer press, or entering the menu mid-pan) the
  game holds every button disabled with its tooltip caption off and unlocks them staggered
  across frames, so the tree holds until the landing button is interactable and labeled - the
  press is followed by silence, then one clean landing ("Exit Game, button"). A keypress
  during the animation skips it, the game's own behavior. Live-verified 2026-07-27 (previously
  spoke "button, unavailable" mid-animation).
- Escape opens settings (the game's own Escape behavior at the title).
- The Confessions submenu is a container swap the count-rebuild picks up.
- Known gaps: the profile button reads the profile name (its "Change Profile" caption is a
  buffer line); the entry landing can speak the transient phase once before settling (the
  game briefly presents the continue state on boot).

### Cinematics panel (`CinematicsPanelScreen`, on the title menu)
Status: **works** (live-verified 2026-07-28)
- The Watch Cinematics panel is a timeline-animated panel on the menu itself (no stack entry;
  it locks every menu button while up - previously it read as the stale menu tree, bare
  unavailable buttons). A dedicated screen outranks both menu readers while
  `IsCinematicPanelActive()`: named from the game's own title, one vertical list of the
  unlocked cinematic buttons then the panel's Back. The takeover holds until the landing
  button is interactable and labeled (the open timeline fades the buttons in).
- Escape closes through the game's own `CloseCinematicPanel`; the menu re-announces once its
  close animation unlocks the buttons.
- Playing a cinematic switches the game to CINEMATIC mode: the screen stands down and the
  keyboard is released for the game's own skip flow (any key shows the skip dialog, holding
  Space for a second skips - device-verified). On the revert the game itself has closed the
  panel, so the menu re-announces.
- Known gaps: the End cinematic button (unlocked by a "body" boss victory) is swept
  generically but was inactive on the dev profile, so it is unverified.
Status: **works** (live-verified 2026-07-26; save select built, untested - no kingdom save on
the dev install)
- The kingdoms scene loads additively over the title menu (the mode stays MAIN_MENU, nothing
  lands on the screen stack); this screen outranks `MainMenuScreen`, which stands down while
  the scene owns the menu. Three phases rebuilt in place: the entry menu (Continue Kingdom
  where a save exists, New Kingdom, Back, the game-type description), the save-select list
  (each save reads its name plus the widget's own day/difficulty/map labels; Enter loads
  through the widget's click path, Shift+Enter opens the game's delete confirmation), and the
  creation wizard.
- Wizard steps read one at a time, landing on the new step's first element after the game's
  cross-fade: the name field (Enter starts the game's own edit flow - every key then goes to
  the field, typed characters echo, deletions speak "x deleted", Enter accepts and re-reads
  the field, Escape cancels), gang cards (single-select, "selected" re-announce; the pick also
  queues the game's own narration), the gang disclaimer text, map toggles (each map's blurb
  tooltip in the buffer), difficulty presets (named from their definition id; the last slot
  holds the game's Custom copy and takes the game's "Custom" caption) with the preset stat
  rows as readouts ("Day Limit. 60. days", explanation tooltips in the buffer), then the
  wizard's own Continue/Back.
- Escape drives the game's `TryGoBack` end to end: save select closes, each wizard step steps
  back (the name step exits the wizard), the entry menu returns to the title menu (which
  re-announces itself).
- While typing, all mod keys pause on the game's own `IsInputtingText` flag (device-verified:
  an injected Down mid-edit moves nothing); the pause also covers the game's other rename
  fields.
- Known gaps: the save-select phase and the custom-difficulty editing widgets (the row
  dropdowns/sliders that appear once Custom is picked) are modeled but not live-verified; the
  mods step is swept generically and unverified; the "creating kingdom" hand-off to DRIVING is
  unmodeled (the road screens take over after the load).

### Settings (`OptionsScreen`)
Status: **works** (live-verified 2026-07-23)
- Tab selector + active tab's rows in one vertical flow; rows: `OptionsItemBhv`
  toggles/sliders (labels/tooltips from loc keys), bespoke widgets (language dropdown verified)
  generically. Toggle round-trip and value re-announce verified.
- Dropdowns open an option popup on Enter (live-verified 2026-07-28 on the graphics tab,
  logical and device-level key paths): the dropdown label then the first option (the current
  choice is never marked or sought), Up/Down move with no wrap, Enter commits (the game's own
  onValueChanged) and Escape cancels - both close by re-reading the restored dropdown row, so
  a commit reads back its new value. The game's own list is shown/hidden alongside
  (`TMP_Dropdown.Show`/`Hide`), and a screen change tears an open popup down.
- Remembered tab verified across close/reopen, including the corrective re-announce after the
  game's open animation stomps the tab back to the first one.
- Escape closes in one press from both the title menu and pause (the game's own Escape is
  two-stage on mouse+keyboard; we fold it).
- **Mod settings tab** appended after the game's tabs (live-verified 2026-07-27): mod-authored
  rows instead of swept widgets, currently the announcement separator - a free-text field
  (`TextEntryElement` + `ModTextEdit`). Enter opens the mod's own typing mode - "editing,
  enter when done" then the bare-Enter outcome spoken as a hint (the suggested value is spoken
  rather than prefilled, so typing starts clean) - with layout-aware characters echoed,
  Backspace erasing, and Escape cancelling with a full row re-read. Enter commits and the new
  separator applies to every announcement immediately; committing nothing resets to the
  default (", "). The session dies silently with its owner (closing the screen under an open
  edit never wedges the keys - live-verified), and the IME is enabled per session so CJK
  composition can type into the mod-drawn field (composition owns Enter/Escape; not yet
  live-verified with a CJK IME).
  Persisted through BepInEx config (values quote-wrapped so a separator's edge spaces survive
  the config parser's trim), verified across a game restart. The value reads spelled character
  by character (a separator is punctuation, inaudible whole). While the mod tab is up the
  game's own tab state is untouched; a mouse click on a game tab leaves it.
- Known gaps: keybind rows read as bare buttons (no rebind flow); DEBUG-tab filter field
  unhandled; sliders speak normalized percent, not the game's display value; the mod tab is
  invisible to sighted users (no game-side tab button is injected).

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

### Embark staging (`EmbarkScreen`, EMBARK mode)
Status: **deployed** (depart-only case live-verified 2026-07-25; relationship rows unexercised -
they need a mid-run embark with new affinities)
- The scene between the crossroads (or an inn) and the drive: an intro plays, then the game
  waits for the depart press - previously dead air (a mode surface with an empty screen
  stack), where a sighted player's keys fell through to the game unspoken. Named "departure".
- One element per pending hero relationship (`EmbarkRelationshipBtnBhv` rows are
  portrait-only; the element reads both heroes' names from the connection's actors, and the
  relationship's own localized name as the value once applied). Enter is the game's own
  press: it commits the pending relationship and plays the game's reveal sequence. The
  apply-all button reads when the game shows one (reveal-relationships option, 2+ rows).
- The depart button reads the game's own binding ("Continue", or "Continue: <region>" when
  a destination is set), "unavailable" while relationships are still pending. Enter drives
  the game's keyboard path, which self-validates: with pending relationships the game
  answers with its own reminder dialog (read by the dialog screen) instead of departing.
- Escape opens the pause menu (the game blocks it itself once departure is underway).

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
  trinket slots. Resistances, quirks, skills, combat items and trinkets are one horizontal
  row each (Left/Right within a row, Up/Down between sections).
- Inline effect glyphs in game text (tokens, dots, heal/buff/debuff and stat icons) are spoken
  as words: token and dot names resolve through the game's own `token_name_*` / `dot_name_*`
  strings; the icons with no name string anywhere in the game (heal, buff, debuff, stress,
  disease, speed, HP) carry authored words; any other icon speaks its humanized sprite name
  rather than silently dropping ("-2 speed" on a trinket, not a bare "-2"). Known-decorative
  glyphs (the hero-seal mark) are the only ones dropped. Applies pipeline-wide - every buffer
  and announcement benefits.
- The **Relationships tab** reads each partner row as a dedicated element
  (`RelationshipRowElement`): the partner's name with the affinity readout the sighted
  banner shows on the focus line - the band word and pip meter ("Paracelsus, button,
  Neutral, 9/20") while affinity builds, or the formed relationship's name (plus remaining
  days in Kingdoms) once one exists - all live from the row's own data bindings
  (`affinity_name` localized, `pip_value`, the game's own Kingdoms gate for the duration).
  The full affinity tooltip (band description, formation-chance breakdown with per-quirk
  contributions) is the buffer, line per line. Enter is the game's own click - it moves the
  sheet to that partner - and speaks the destination hero's name so the switch is never
  silent (the landing row announce can read one frame stale off the reused ring widgets;
  the hero name is the reliable signal). Live-verified 2026-07-24: all rows with values,
  buffer chain, jump both ways with the hero announce. Unexercised: a formed relationship
  (all Neutral in the test run; the tooltip mechanism is shared so buffers carry its
  description) and the Kingdoms duration line. The unviewed-change notification icon is
  deliberately unspoken - the game clears it the moment the tab opens, so one glance is
  all sighted players get too.
- The other tabs (Conditions, Story, Cosmetics) read as a generic sweep of the tab
  panel's labeled selectables, with the panel's own text - or "empty" - as the floor; verified
  live: Relationships "empty" pre-run, Conditions "Memories", Story its unlock hint, Cosmetics
  its palette buttons.
- Verified: equip toggle round-trip (on/off/on), hero switching rebuilds all content, tab
  switching (both our selector and the game keeping the tab across hero switches), Escape
  closes through `HideCharacterSheet` with the crossroads re-announcing, physical **I** key
  entry from a hero slot.
- Equip slots (trinkets, combat items) are `EquipSlotElement`s: occupied slots read the
  item's own title from the model, empty ones their caption, and activation speaks the
  landed state (live-verified 2026-07-24 via the inn equip flow, both directions).
- Known gaps: hero rename (the name input field and edit button) is not modeled; the cosmetics
  tab is floor-level (palette slots read as bare numbers); the game's own tab hotkeys and
  tooltip-view mode are not used.

### Generic floor (`GenericScreen`)
Status: **works** (live-verified 2026-07-23, originally on the hero sheet before its dedicated
screen existed)
- Any pushed SCREEN stack entry with no dedicated reader gets a generic sweep of its labeled
  selectables, so no surface is dead air. Registered above the mode screens (a pushed screen
  covers the scene) and below the dedicated stack screens. Driving HUD widgets (minimap,
  goals - non-SCREEN stack entries) are excluded so free driving is never captured.
- Escape closes a `SubScreenElementBhv` panel through its own `CloseSubscreen`, a raw
  `TryCloseScreen` otherwise: a hub re-enables its own controls only in the panel's close
  flow - a raw pop of the altar's stagecoach-tracks panel ("The Intrepid Coast", on the
  generic floor) left every altar region marker disabled (observed live 2026-07-24; the
  in-place repair is the game's own `CheckToEnableSubScreenButtons`).
- **Results surfaces read fully** (live-verified 2026-07-24 on the inn's End Expedition
  screen, "Every League, a Lesson."): the score-row prefab these screens share
  (`GameOverScoreLabelBhv`) reads as a readout composed like the sighted row - the game's
  reason label plus its number ("Candles Found: 3"; a 0 is what the visual cross mark
  means) - with the row's explanation tooltip in the buffer, followed by the run total
  ("total 5"), which the visual panel shows only as a bare number beside a candle icon
  (no game caption string exists; the total reads the game-over flow's pre-composed line
  as-is when the binding holds one). The same code serves the game-over and Kingdoms
  results screens (deployed, unverified there). Collect Hope reads as the ordinary button
  it is.
- **Rebuilds are silent while the surface grows**: the results screens animate their score
  rows in one at a time, and each arrival re-populates the tree. Elements are keyed to
  their live widget and reused across rebuilds (the rebuild check is an instance-id
  signature, not a count), so the focused element survives and nothing re-announces - the
  game-over screen used to queue "Continue, button" once per arriving row (observed live
  2026-07-24). Focus falls and announces only when the focused widget itself is gone.
- Known gap: the screen name is read at entry, before a results screen's late-bound title
  has text - the game-over screen announced itself as "Continue" (its first readable
  label at that instant) rather than its title.

### Player inventory (`InventoryScreen`, the standalone "Inventory (I)" screen)
Status: **works** (live-verified 2026-07-25 on the road: entry landing, tab, and the
Escape close path back to the underlying screen; the panel body is the inn's
live-verified reader extracted verbatim. Previously fell to the generic floor, which
announced itself as "Sort" and read item slots as their bare stack count)
- The game's inventory screen as pushed on the road, at the crossroads, and from the loot
  screen. Nothing but the shared bag panel (`InventoryPanel`, the same reader the inn hub
  embeds): the filter as a tab (Left/Right apply the game's own icon-only filter buttons,
  captions from their loc keys), slot count ("15 / 20") and wallet rows (Relics/Mastery/
  Baubles, captions from their tooltips), the sort button (press confirms "sorted by
  type"), one element per carried item (title and stack, full tooltip in the buffer,
  Shift+Enter discard), the free capacity as one line, and Space grab-and-place with
  Shift+Space single placement. Escape drops an armed grab first, else the game's own
  `HidePlayerInventory` close.
- The inn outranks this screen by registration order and keeps its inline copy; dedicated
  station screens above both take their own surfaces.

### Combat (`CombatScreen`, COMBAT mode)
Status: **works** (live-verified 2026-07-24: two full rounds fought to Victory - skill picks,
target picks, kills, turn handoffs, free-action stance swap - with the expanded event set and
header row)
- Layout, top to bottom: the header row (Left/Right within it) - the battle status ("round 1,
  Audrey"; torch value, wave count in chained fights, round detail, and retreat odds as buffer
  lines), the **turn order** ("turn order, Sahar, Audrey, Widow...", current actor first, read
  live from `QueryTurnOrder`; the order is rolled per round, so the current round's remainder
  is all the information the game itself has; a name shared by several living enemies speaks
  with its rank - "Lost Soul 1, Lost Soul 3" - matching the game's only pointer to the
  specific one, the model highlight under a hovered portrait; the numbers are read live, so
  they follow deaths and position changes, and drop when one survivor remains), the **battle
  goal** (the game's
  `battle_goal_<config>` string, present only in fights that carry one), and the **battle
  modifier** (title from `battle_modifier_title_<id>`, present only in fights that roll one;
  its tooltip title and effect/buff descriptions are buffer lines) - then the enemy strip, the
  party strip (both
  rank-ordered; labels are name + Rank + HP read live; a monster's name is its data id's loc
  string, the same source as the game's turn-order tooltips), the skills row (horizontal, with
  the game's own "Uses: N" limit text and the game's `invalid_skill_reason_<type>` wording
  when a skill cannot be used - wrong rank, cooldown, out of uses - instead of a bare
  "unavailable"; when the game grants an always-equipped copy of a skill the player also
  equipped it shows two identical buttons that select the same skill - the mod reads only the
  first and ends its buffer with "also granted as a bonus skill"), then the
  commands row (Move, Pass, and Retreat when the game offers it).
- The turn: Enter on a skill runs the game's own pick handler and announces "select target".
  Landing on a combatant then plays a validity beep (660 Hz triangle for a valid target,
  440 Hz for an invalid one, `assets/audio/combat`), only when the validity CHANGED from the
  previously focused combatant - runs of same-validity targets stay silent. An invalid
  target's line leads with the derived reason (out of range, allies only, stealthed...,
  mirroring the game's own target-validity walk, which sighted players see only as dimming);
  a valid target's line ends with the game's own precomputed preview (`QuerySkillPreview`:
  "85% hit, 5% crit", or the heal range on friendly skills; "intercepted by X" when a
  guardian will absorb the hit, "riposte 3-7" when the pick draws a counter; "removes /
  steals / converts X" for the recipient's tokens the pick would strip - the lists the game
  previews by flashing tray icons, named only when held. Dot cleanses have no game preview
  and are deliberately not spoken, keeping parity with the sighted view - the removal reads
  in the skill's own text; a conditional heal below its HP threshold previews as 0 and
  stays silent, matching the game's hidden heal bar segment). Enter on one
  sends the game's
  own actor-pick event to execute. Escape cancels target-select first, else opens the pause
  menu.
  Turn lines ("round 2, Audrey") are spoken outright on every turn change - focus can sit
  anywhere - and logged to the combat buffer once.
- Combatant buffers: HP, stress (heroes), then one line per token (hidden tokens filtered
  by the game's own `IsHidden` gate - they are internal logic-control state whose loc text
  is a "please file a bug" placeholder), per dot, and per combat
  buff (filtered to `IsEligibleToShowAsCombatUi`, e.g. Preparation's "On Riposte: heal Self
  10%"), all from the game's own describers. Skill buffers: the full skill card (shared
  `SkillCard` composer with the hero sheet).
- **Battle events** are announced as they happen (queued, so narration stacks in order) and
  kept in the **combat buffer** (Ctrl+Left/Right; follows the latest line; empties when the
  battle ends). Display gates mirror the game's own pop-text handlers. Covered: damage taken
  ("Lost Soul took 4 damage"; number dropped at 1; ", crit" appended on crits), heals (with
  crit variant), misses and dodges from the finalized skill results ("Woodsman missed
  Paracelsus" / "Audrey dodged"), stress damage and relief ("Dismas gained 2 stress" /
  "Audrey lost 1 stress"), meltdowns (the game's "resolve is tested" line plus the outcome's
  own name), deaths, death's-door falls and survivals ("Woodsman resisted the death blow"),
  what enemies do ("Lost Soul used Chomp on Paracelsus") - never the player's own skill
  picks - token, dot, buff, and quirk applications ("Dismas gained Crit", the game's own
  names and count format, honoring its pop-text visibility gates; buffs speak their stat
  text), token consumption and negation ("Sahar spent Speed" / "Sahar lost Weak"), resisted
  effects ("Woodsman resisted Blight"), retreat outcomes and wave starts and the final round
  (all three via the game's own pop-text strings), wounds, affinity changes ("Dismas and
  Paracelsus, affinity +1"), barks ("Dismas: I line 'em up..."), hero objective completions,
  and tutorial/message toasts shown over combat ("tutorial, Enemy Death Armor"; Harmony
  postfixes on `ToastManager`, the one toast surface with no event).
  Verified live 2026-07-24: turn order readout, blank goal hiding, buff buffer lines, crit
  damage, miss, stress damage and relief, token spend and loss, death-blow resist, affinity
  tick, barks, tutorial toast, always-spoken turn lines, wave count suppressed in a
  single-battle fight.
- Known gaps: dodge/heal/meltdown/retreat/final-round/wave-start/wound/quirk/objective/message
  -toast lines are deployed but not yet observed live (their handlers share gates and
  composition with the verified ones); the goal readout is unverified in a fight that has
  one; relationship skill markers rely on the skill card's actor-aware result strings and are
  unverified with an active relationship; a combat item rides the skill bar as a regular
  skill button but no hero had one equipped to verify; a token id with no name key anywhere
  ("blind-line") reads as its humanized id; Move is untested against position targeting; Pass
  briefly announces "select target" before auto-resolving; the retreat element only
  (dis)appears on turn-boundary rebuilds; stealth/corpse/summon edge cases unexercised; the
  **gang escalation tooltip** (Kingdoms sieges,
  `m_escalationTooltip` on `BattleInfoUiBhv`, shown via the More Info hold) is not modeled -
  Kingdoms-only, needs a siege to design against; the battle modifier readout is deployed but
  unverified (no modifier rolled in the fights seen so far); target beeps, invalid-target
  reasons, and the hit/crit preview are unverified against friendly skills and stealth
  (guard interception and riposte verified live against the preview cache, spoken as
  suffixes); battle-end cleanup fires "Corpse died" lines (real death events for the
  corpse entities - noise at the end of a won fight, informative mid-fight).

### Inspector (`AcademicScreen`, over combat)
Status: **new** - the game's academic view (hold-Alt / middle-click for sighted players),
driven through the game's own show event so the camera, fog of war, and its gates follow.
- **I** toggles it on the focused combatant (the acting hero when focus is not on a
  combatant; "unavailable" when the game refuses - enemy turns, mid-animation). **A / D**
  cycle combatants battlefield-order without leaving the view (the game's own keys for it);
  the new subject's name is spoken and focus keeps its row. Enter (or C) on a party hero's
  identity line opens their character sheet; Escape or I closes, and the game's own
  force-close (combat resuming) falls back to the combat screen, which re-announces.
- Layout, top to bottom, all read live from the model through the game's own describers:
  the identity line (name, "blessed" on ordained enemies, HP, stress on heroes, speed;
  death's door and the boss-blessing description as buffer lines), the studied **skill
  list** (enemies: round skills first, then turn skills, each with the game's own token-view
  card in the buffer - ranks/targets, the tokens and dots it applies, melee/ranged - plus
  flavor description, token ignores, and use conditions; the full effect renderer is a
  player-skill surface whose enemy-only internals, AI class changes, read as raw ids;
  skills the player has never seen use the game's own "???" hidden strings; heroes:
  equipped skills with the full skill card, remaining uses and cooldowns), hero **conditions** (class conditions, condition-tagged
  buffs, stagecoach effects, the wound line), **trinkets** (enemies and Kingdoms allies
  carry visible ones), the **resistance grid** (every resist with the game's immune and
  death's-door special cases; per-source breakdown in the buffer), then **tokens, damage
  over time, buffs, debuffs** (empty sections vanish; hidden tokens - the game's internal
  logic-control state with "please file a bug" placeholder text - are filtered out, the
  same IsHidden gate the game's own token icons apply).
- Known gaps: unverified against an ordained (blessed) enemy, a stealthed enemy, and
  Kingdoms militia allies; resist percent formatting assumes the model's 0-1 fractions.

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

### Travelogue (`InnResultsScreen`, inn-arrival recap on the stack)
Status: **works** (live-verified 2026-07-24: arrival restore, full arrow walk)
- The inn-arrival run recap (`SubScreenBiomeResultsBhv`; the hub's Travelogue button reopens
  the same surface). Reads like a modal: one focusable text row per run-log entry (the
  game's own rendered lines - "The resolute Companions reached The Torch & Crown", "2
  Candles gained for reaching the Inn!"), then the Loathing meter's tooltip as a readout
  ("The Loathing Abates, Prologue"), then Continue (arrival only; the game hides it on a
  reopened travelogue). Continue activates through the button's own submit; Escape runs the
  screen's continue flow when a continue button stands, else the game's own close.
- Known gaps: the innkeeper portrait button (unlabeled, flavor) is not modeled; a reopened
  travelogue (from the hub) is unexercised live.

### Inn hub (`InnScreen`, INN mode)
Status: **works** (live-verified 2026-07-24 at the prologue inn: name, hero row, stations,
full inventory walk, item tooltip buffers)
- Named by the inn's own title (`InnBhv.GetInnInstance().Name` already holds the localized
  title; authored "inn" fallback). Layout, top to bottom: the regions-to-mountain readout;
  the **hero rest strip as a horizontal row** (`RestHeroElement` over each `RestItemSlotBhv`
  - name with HP and stress from the live actor, the slot's status tooltip as buffer lines,
  Enter through the slot's own submit - which in Kingdoms is the game's own path INTO the
  Select Replacement Hero screen); the **stationed-heroes row** (Kingdoms: the portrait
  strip by the inn title, from the same `InnStationedActorBhv` widgets sighted players see -
  each reads its class-name tooltip, Enter opens that hero's sheet the way the game's
  right-click does; the pool is empty at expedition inns so the row vanishes from the
  walk); the station buttons (captions from their tooltips;
  the prologue inn genuinely offers only Travelogue and End Expedition - the bar rebuilds
  per inn and later inns add the shops); then the inventory panel: the filter, "slots 5 /
  20", and wallet rows ("Relics, 40") as readouts, the sort button, one element per carried
  item (`InventoryItemElement`, shared with the loot screen: the item's own title and stack
  - "Candle of Hope, 3" - full tooltip in the buffer), and the free capacity collapsed to
  one live line ("15 empty slots"). Escape opens the pause menu.
- Sort speaks "sorted by type" on press (the game has exactly one sort - item type, then
  name; no modes). A sort re-populates the pooled slot widgets with all-new instances, so
  the item list rebuild is keyed to an instance-id signature, not a count - and the
  inventory's frame elements (filter, count, wallet, Sort itself) sit outside the rebuilt
  container so focus survives the press. Verified live: two consecutive sorts, items
  walkable immediately after each.
- The filter reads as a tab ("All Items, tab"): Left/Right apply the game's own tab
  buttons through `InventoryUiBhv.ApplyFilter` - the mouse path; the tabs themselves are
  icon-only and invisible to a text sweep, and the game's own controller cycling
  (PrevTab/NextTab) is gated on EventSystem focus we deliberately never grant. Hidden
  (HideIfEmpty) tabs drop out of the live list. Verified live: cycling all five tabs both
  ways with clamped ends, the item list re-filtering per tab (Trinkets showed exactly the
  two trinkets), and the walk restored under All Items.
- **Shift+Enter discards** the focused bag item (the game's shift-click; the whole stack,
  instantly - the game confirms nothing except its own last-trophy safeguard). The element
  advertises the action only where the game allows it (`m_canDiscard`, player bag slots);
  anywhere else Shift+Enter answers "unavailable". Player-verified live. The press now
  speaks its outcome from the model - "discarded X", or **"sold X"** when a seller is open
  (the game's own handler sells ONE item per press in that state instead; the wording
  follows the same `GetIsSellingActive && HasSellCost` branch the handler takes). Silence
  means the game's confirmation dialog took over, and it announces itself. Sell wording
  deployed but unverified live (no seller at the prologue inn; expedition selling is gated
  on the hoarder option).
- **Space grabs and places inventory stacks** (`ItemGrab`) - the keyboard face of the
  game's item drag and drop, same key and feedback as the crossroads hero grab: Space on
  a stack speaks "grabbed X", Space on the landing target places the whole stack, and
  **while a grab is held Shift+Space places a single item off it** (the game's
  split-stack drag), keeping the grab held so repeated presses keep splitting until the
  stack runs out (then the grab ends with the landing line; Shift+Space never initiates -
  unarmed it answers "unavailable"). Targets: another stack (same item combines - the
  merge the mouse gets by dropping; different item swaps in place, no free slot needed -
  the full-inventory exchange) and the **"N empty slots" capacity line, meaning this
  inventory's free space** (a single placed there accumulates onto an existing partial
  stack before opening a new slot, and a fresh stack opens the LAST empty slot, so it
  reads at the bottom of the list right above the capacity line the cursor sits on).
  Placement mirrors `InventoryItemBhv.DefaultSwap` at the model level
  (`ItemInventory.SwapItems` / `TakeItemQty`, then `EventInventoryItemSwapped` like the
  game's own drop), with the game's `AcceptsItem` rules honored across inventories; a
  split onto a different-item target answers "cannot place here" rather than inherit
  DefaultSwap's whole-stack-swap fallback. Same-slot Space or Escape cancels ("grab
  cancelled"; Escape only falls through to the pause menu when no grab is armed). The
  landing speaks the placed stack's title and new size from the model. Live-verified
  2026-07-24 (dev driver + player keys): whole-stack move, split, accumulate,
  split-until-empty ending the grab, merge, swap both directions, both cancels.
- **Item-list rebuilds re-home focus** over the widget the cursor sat on (a placement, a
  sale, a restock rebuilds our elements; the cursor no longer falls to the top of the
  screen), silently - the action's own feedback is the only speech.
- **Equipping rides the game's own slot-select flow, end to end** (live-verified both ways):
  Enter on a bag trinket/combat item makes the game itself open the hero sheet in
  slot-select mode; Enter on a sheet equip slot runs the game's `Swap()` (equips; a held
  item swaps back to the bag), and Enter on an occupied slot auto-transfers it off (unequip).
  Escape anywhere cancels the mode and falls back to the inn. Sheet equip slots are
  `EquipSlotElement`s: the label is the equipped item's title read from the model (current
  the same frame the swap lands, where the widget text is a frame late; empty slots read
  their "Equip Trinket" caption) and activation speaks the landed state back ("Minor Gilded
  Mind" on equip, "Equip Trinket" on unequip). Bag-to-bag rearrangement has no logical
  handler in the game at all (mouse-drag only), so it is deliberately out of scope; REST
  items use a select-then-apply-to-hero flow instead of slot-select (unexercised live).
- Bag position carries no meaning (no adjacency; the game's own Sort reshuffles), so the
  3-column visual grid is deliberately flattened to an occupied-only list; empties exist as
  the one capacity line. The game's slot-swap flow (`IsSelectingItemSlot`) accepts any
  same-inventory slot as a target, so a future grab-and-place flow needs only one "empty"
  destination, never a specific cell.
- The embark button reads once: the game nests a clickable "Select a Route" overlay button
  inside the disabled Rest/Embark button (a mouse-only nag whose caption duplicated the
  station bar's own Select Route button), so the station sweep skips any selectable nested
  under another selectable - the overlay's caption still reaches the Rest element's buffer
  through the parent's tooltip scope.
- The hub deliberately outranks the generic floor: the inn keeps its inventory panel
  (`screen_inn_player_inventory`, an `InventoryUiBhv` stack entry) as the top stack entry
  while the hub is up, and the floor would otherwise capture just that panel and strand the
  station buttons. Any station sub-screen pushed above it hands the surface to its own
  reader (travelogue dedicated, the rest the generic floor for now).
- Known gaps: rest-item application onto heroes (the game's select-then-apply flow; our elements
  drive the right handlers but no REST item was owned to verify) and shops' richer inns are
  unexercised (only the two-station prologue inn verified). The grab flow's cross-inventory
  half (bag to **inn storage**, where `AcceptsItem` blocks undiscardables) is unexercised:
  storage is a Kingdoms feature and only the bag exists at an expedition inn - it needs no
  new code (grab is target-generic over `PlayerInventoryItemBhv` slots) but wants a live
  pass when a Kingdoms inn is first modeled. Grab deliberately excludes loot and store
  widgets (their Enter flows already transfer). The "new item" glow is unspoken (its model
  flag is consumed on first render - `Refresh` calls `SetViewed` - so the live signal would
  be the background director's loop; deliberately skipped as cosmetic).

### Select Replacement Hero (`InnReplacementScreen`)
Status: **deployed, needs live pass** (previously read by the generic floor: class names
only - no hero names, no at-this-inn markers)
- The kingdoms hero swap screen (`InnReplacementScreenWidgetBhv`, pushed by Enter on a rest
  slot): the Stationed Hero Effects readout first (its tooltip - what a hero stationed here
  gains - as buffer lines), then one row per candidate (`InnReplacementRowElement`): the
  hero's **name then class** from the row's own bindings ("Paracelsus, Plague Doctor"; the
  random row reads its "Random Hero" label alone), **"at this inn"** as the value exactly
  where the game shows its inn marker on the row (`m_innActorObj`, driven by the current
  map cell's `ActorGuids`), and the row's add/station tooltip as buffer lines - "Add hero
  to party", or "Station hero" on the last row, which is the current party hero and whose
  Enter REMOVES them to the inn (the buffer is the distinguisher, matching the sighted
  hover tooltip). Enter is the row's own submit; Escape closes through the screen's own
  teardown.

### Inn stations (`StoreScreen`, `MasteryScreen`, `WainwrightScreen`,
`RouteSelectScreen`, `RelationshipMatrixScreen`, `InnUpgradesScreen`)
Status: **works** (live-verified 2026-07-24 at the first Denial inn), each named by the inn
header's own station title (which retitles a beat after entry, so the entry announce can
speak the inn's name once - known cosmetic race). All close through their own
`CloseSubscreen` on Escape.
- **The Provisioner** (`StoreScreen` over `InnStoreUiBhv`; the same screen serves road
  merchants - see the Hoarder under Driving): wallet rows, then the store slots - item
  title, price, and stock from the model and the game's own price text ("Bear Trap,
  button, relic 6, 2"; a sold-out slot reads the game's "Out of Stock!"), full item
  tooltip in the buffer - then the player's bag (shared elements: items, free-capacity
  line) so **Shift+Enter sells one per press** with the "sold X" wording. Both lists
  rebuild on an instance-id signature with focus re-homed (pooled widgets recycle on
  every transaction).
  Enter buys through the game's own validated purchase; a landed buy speaks the slot's new
  state, a failed one the game's insufficient-funds line. Live-verified: full walk, one
  purchase (A Glimmer of Hope, stock and outcome spoken). Sell-one press not yet
  player-verified on this screen (identical wiring to the verified inn-hub discard).
- **Mastery Trainer** (`InnUpgradeSkillsBhv`): the hero header (name + "mastery points N";
  Left/Right page the party via the trainer's own arrows), one element per skill (the
  game's own name with its upgraded-skill glyph spoken as words; states "mastered" /
  "selected" / "unavailable"; full skill card in the buffer), the path seal, "Change Path"
  with its cost (caption from its tooltip - the visible text is only the cost), and Reset -
  whose visual is a hold gesture, so the element drives the real `OnResetPressed`. Enter
  queues a skill through the trainer's own `TrySelectSkillToUnlock` (the mouse holds); the
  rebuild announces the new points. The path panel stays permanently active with a
  CanvasGroup riding visibility, so the view split keys on `blocksRaycasts`; the path view
  reads the comparison text (named children only - the panel carries unbound template
  labels) plus each path option and the purchase button. Live-verified: walk, hero paging
  wiring, queue ("selected", points drop), Reset (queue cleared, points restored).
  Unexercised: Apply/commit (the batch confirm), an actual path purchase, hero paging with
  a full party.
- **The Wainwright** (`StageCoachConfigUiBhv`): the coach's name from the model (renaming
  is unmodeled), wallet, the game's own composed stat lines ("Cargo Slots: 20",
  "Armor: 2/2", damage explanations in the buffer), a "repair, baubles 8" button per stat
  (the game's own transaction; `cost_` currency glyphs now speak - the faction glyph as
  the authored "baubles", no game string spells it), the livery cycler, and the upgrade
  slots as equip slots (altar-locked ones carry their lock text). Live-verified: full
  walk. Unexercised: a repair press (stats were full), equip/unequip on this sheet.
  The same class covers the read-only sheet the road opens on Z ("The Stagecoach" - the
  screen is named by the game's own per-context title, derived from the game's loc keys
  on the entry frame since the sheet stamps its DataContext a frame after topping the
  stack): no wallet, repairs, or livery there, and slot edits are refused by the widget's
  own editable gate. Live-verified on the road 2026-07-31: entry announce, full walk
  (name, stats, all seven slots with their unlock/lock tooltips in the buffer), Escape.
- **Relationships** (`RelationshipMatrixScreen` over `SubScreenRelationshipMatrixBhv`,
  Kingdoms only): the anchor hero first (name and class - the panel the portrait grid pivots
  around), then one element per other roster hero - name and class from the actor model (the
  tiles' identity is portrait-only, no text), then the anchor's relationship as the tile
  draws it: the band word with the affinity meter ("Neutral, 11/20"), plus the formed
  relationship's remaining days while the tile shows them. Enter re-anchors the matrix
  through the tile's own click; the pooled rebuild re-lands on the anchor readout, which
  names the new anchor. Live-verified 2026-07-26 at the Alpenglow inn: walk, a re-anchor
  (readout renamed, tiles pivoted to the new anchor), Escape to the hub, reopen from the
  hub's button. Unexercised: a formed relationship's name and countdown (all pairs were
  Neutral). The game's right-click shortcut to the partner's character sheet is unmodeled.
- **Inn Upgrades** (`InnUpgradesScreen` over `SubScreenInnUpgradeBhv`, Kingdoms only;
  live-verified 2026-07-26 at Alpenglow): a tabbed screen - the category tab first,
  Left/Right stepping the tab group's OWN association order via its click handler (the
  prefab's page order differs from the panel's handler-method order, and the group's
  m_activeIndex is not authoritative - its click loop overwrites it per association - so
  the current tab is read off the visibly active page; both directions verified symmetric),
  the game's materials line, then the active category's tree as `InnUpgradeNodeElement`s
  (shared with the map inn panel's tree). A node reads: name, owned or the game's composed
  cost, then WHY it cannot be bought, mirroring what the tree shows - "needs X, Y" from the
  prerequisite wiring (verified: single and multi-prereq chains), the game's own
  Insufficient Funds line for a red cost (verified), and the level-restriction banner text
  for out-of-tier rows (code path present; this inn had no level gate to observe).
  Description in the buffer, plus the category's verbose flavour on the ultimate node
  (verified: the Barracks "extensive training regimen" text). Enter purchases through the
  node's own gated Unlock. Escape closes through the station's sub-screen flow.
  Unexercised: an actual purchase (commits the save).
- **Select Route** (`SubScreenBiomeChoiceBhv`): one element per offered route - the
  destination's own name, "selected" state, goal/modifier/reward tooltips in the buffer,
  Enter marking the choice through the game's own submit - or "empty" when the inn offers
  none. Live-verified ONLY in the empty state: this Denial inn wants 2 biome choices but
  rolled zero, and `GetCanEmbark` is false - either a stuck save state (many dev restarts
  mid-inn) or choices that appear after an inn step; the populated reader is model-built
  and untested. **If departure refuses, this zero-choice state is why.**

### Kingdom overworld map (`KingdomMapScreen` + `KingdomInnPanelScreen`,
`KingdomBiomePanelScreen`, `KingdomEventPanelScreen`, `InnStorageScreen`)
Status: **works** (live-verified 2026-07-26 on the Drakia save, day 1); actions that commit
the save (travel, pass day, engage siege, hero transfer, boss travel) are wired through the
game's own handlers but deliberately unexercised.
- The map is NOT a stack screen: opening it pops the inn's inventory entry and shows its own
  presentation layer. `KingdomMapScreen` matches `KingdomBhv.IsMapOpen()` with no SCREEN
  stack top (any pushed screen - cell panel, hero sheet, storage - covers the map and reads
  instead), registered above `InnScreen`. Escape = `KingdomPresentationBhv.CloseMap()`,
  which the game itself refuses during day-turn cinematics (spoken "unavailable").
- **Grid cursor** (first Tab stop, Panel root - Tab crosses cursor / header / sieges /
  heroes): arrows step the 9x9 cell grid, Home returns to the stagecoach, landings mirror
  into the game's own selection (camera pan, highlight, travel-arrow preview - all
  presentational) when the cell is selectable. A cell reads view-first, so hidden content
  cannot leak by construction: the pre-reveal boss cell and its 8-neighbour ring deactivate
  their view objects and read as "empty"; treasure reads from the cell's own bindings (the
  game hides it on the occupied cell); siege strength speaks only the 3-band bucket the icon
  shows; a boss-subtype biome is nameless on screen and stays so. Cell line: name,
  stagecoach here / travel scheduled / reachable (user-input states only), stationed hero
  names, siege (band + days), treasure (+ duration), biome markers (cursed/quest, kill
  contract title, reward offered, upgraded). Buffer: the cell's own tooltips ("The
  Stagecoach is here", hero classes), fast-travel state, hero name+class lines, grid
  position. Enter = the game's `EventKingdomActivateMapCell` (gated on
  `IsInUserInputState`): inn/camp opens the inn panel, biome the biome panel, boss travels
  or shows the game's blocked dialog.
- **Header** readouts: day, PASS DAY button, current-event button (opens the event panel),
  escalation level (widget tooltip as buffer), timeline (last-day line; buffer = the marked
  days only: escalation surges, quest steps, final day). **Sieges**: one element per active
  siege - inn name + days; Enter jumps the cursor to the cell. **Heroes**: party + reserve
  rows (name, class, travel-scheduled state; Enter on a reserve hero enters the game's own
  hero-travel mode, gated to WAIT_ON_PLAYER).
- **Inn/camp cell panel** (`ScreenKingdomMapInnPanel`), named by the inn from the model:
  garrison (hero name+class, militia class slots, travel/immobile status tooltips in the
  buffer), defenseless label, travel / fast travel / engage siege / storage buttons as the
  game shows them, the five upgrade tabs (loc-named: Barracks etc.), treasure rewards.
  Enter on a tab opens the **upgrade tree** view: one element per node - name, owned or the
  game's composed cost ("materials 5"), unavailable when locked/unaffordable, description
  in the buffer; Enter purchases via the node's own gated `Unlock()` (the sighted gesture
  is a hold). Escape folds the tree first, then closes (the panel's own two-stage back).
- **Biome cell panel**: name + enemy roster, expedition rewards, upgrades/modifier text and
  kill contract gated on the model (the labels keep template text when unbound), reward
  tooltips in buffers. Informational only - the game offers no actions here.
- **Event panel** (`ScreenKingdomMapEventPanel`, day/event notification): day + title +
  effect + flavour as one element, rewards, close button; Escape via TryCloseScreen (the
  game swallows it during the slow day-intro). Model-built, NOT yet seen live (needs a day
  to pass).
- **Grid Tab wraps** both directions (the map root sets the navigator's `WrapTabStops`),
  and Enter on a siege element hands focus straight back to the grid cursor at the jumped-to
  cell. Panel entry announces read the cell's name from the game's viewed-cell query (set
  before the push), so they are correct from frame one; the landing can still speak one
  transient line before the populate re-land (cosmetic).
- **Garrison reordering** (live-verified: swap spoken as the new order, model confirmed):
  the grab key picks up a stationed hero and places it on another garrison slot - the two
  widgets swap slots and the order commits through `SetActorOrder` via the panel's own
  `GetActorOrder`, the same call its drag release runs. Militia slots refuse the grab;
  Escape drops a held hero first.
- **Inn Storage** (`InnStorageScreen` over the `InnStorageBhv` stack entry, live-verified:
  stored stack read, grab-and-place within storage, model-change rebuild): the inn name,
  the storage list (occupied stacks + free capacity line, shared `InventoryItemElement`
  machinery), the frame's Inventory button, and - when the bag screen is open beneath (the
  inn-hub path) - the full shared bag panel, one grab spanning both inventories. From the
  map path the game shows storage alone; the Inventory button flips views.
- Known gaps: sidebar cursed-regions counter unread; travel-path preview for hero
  transfers is visual only; cross-inventory grab (bag to storage with both lists open) is
  wired through the verified shared flow but that both-open state was not reached live.

### Driving (`RoadSense` + `RouteChoiceScreen`, DRIVING mode)
Status: **built**; cues live-verified by ear (pickup ping confirmed audible), fork menu not
yet reached in play
- Free driving stays UNCAPTURED - the game keeps WASD (W rolls/cruises, S brakes, A/D
  steer, M/I/G/Z/C its own screens). The mod adds an audio layer through its own NAudio
  output (independent of FMOD; `assets/audio`, placeholders replace 1:1): **every
  uncollected pickup in range loops** (one live loop voice each - pan to its bearing,
  louder as it nears, parameter steps smoothed ~5 ms against zipper noise) and **every
  map node in range loops its destination's identity timbre** (shared with the fork menu
  via `NodeCues`; its first appearance also plays once louder as the announcement), each
  re-aimed EVERY frame so steering reflects immediately; a loop cuts the frame its object
  is collected/executed or drops out of range (a 10% exit margin keeps the boundary from
  flapping). The allocating scene sweeps run on a 0.7 s clock only to refresh the
  candidate arrays (measured live: ~43 pickups loaded, 2 within the 80-unit range; 6
  nodes loaded, 1-3 in range - a handful of concurrent voices, mixed under one output
  limiter). Collection plays a blip and speaks the item's own title; road damage plays
  the penalty cue and speaks the combat damage wording (the coach's stop/start is left to
  the game's own driving audio); a junction's banners coming up cue "fork ahead" (once per
  junction).
- Wired 2026-07-25, by ear pass pending: **road edge** (off-center distance against the
  road's half-width from the game's own road geometry; bumps panned to the drifting side
  past 85%, re-arming under 70%); **zone enter/exit** (the game's own road-event zone
  events; exit only while still uncollected - a pickup passed by); **the opt-in prompt**
  (an event that fires only on the game's Interact key - Space/Enter on keyboard - cues
  the prompt and speaks "interact" instead of the zone blip); **ambush**
  (AMBUSH-category event executing); **danger stretches** (the game's inkfire-tile flag,
  enter/exit on the flips); **Loathing** (a DOOM run-value increase).
- The fork menu (`RouteChoiceScreen`) opens when the game's own junction wait halts the
  coach unchosen: routes in left-to-right order read "direction, destination" (the game's
  road-indicator titles; "Unknown" unrevealed - the hidden type is never leaked), each
  focus playing the destination's identity tick panned to its side. Buffers: description,
  which heroes prefer the route, banner tooltips. Enter commits via the banner's own
  OnClick (game audio + narration; the coach then drives itself); Escape dismisses that
  junction back to manual steering (steer at a banner holding W, the game's hold-to-fill).
- **Pickup titles ride the loot toast** (`EventLootToastPresented`): a road grant never
  raises the inventory widgets' loot event (the mod's original hook, dead code on the
  road - found live 2026-07-25 as "collection sound but no name"), so the item's own
  title speaks when the game's corner toast presents - speech only, no mod cue, because
  the game's own pickup sfx already marks the moment.
- **Road transients** (all live-verified 2026-07-31 by firing the game's own paths):
  tutorial and message toasts route by mode through the toast postfixes (combat queue in
  battle, the road pending queue on the road; the patches attach at startup, not on the
  first combat resolve). Hero barks (banter, act-outs - `EventBark`, the same event the
  combat listener rides) speak speaker-prefixed on the road tick. The coach's
  Loathing-resist pop speaks the game's own "LOATHING RESIST" text (the English template
  carries no number slot); the low-flame ambush pop ("The Flame Exhausted") rides the
  combat pending queue outright, because it plays as the ambush battle spins up, and so
  speaks with the battle's opening (queue-level verified; a real ambush is unexercised).
  The corner InkfireBanners carry no text or tooltip - decorative; the danger cues carry
  that state.
- **Road merchants (the Hoarder)** read through the shared `StoreScreen`: the game
  raises the player inventory panel above the `StoreUiBhv` screen and the pair reads as
  one store surface, named by the store's own title - wallet rows, store slots with
  price and stock, the bag with sell-per-press where the game allows selling (the
  Hoarder needs the altar's Enable Hoarder Selling option). Escape exits through the
  store's own done flow, which resumes the drive.
- Known gaps: fork menu unexercised live; coach damage/break and barricade/cleared cues
  still have assets but no wiring (no dedicated game event surfaced - wheels/armor are
  stagecoach items, so a count poll is the likely wire; barricades want live confirmation
  of what spawns as force-stop obstacles).

**Roadside nodes (curios), mapped from the trigger architecture 2026-07-31.** Every node
executes a serialized trigger list, and every interactive one funnels through ONE shared
prompt - `CommonUiBhv.ShowEnterNodeScreen` (`EnterNodeScreenWidgetBhv`, a pushed stack
screen: a single "Search the Cache"-style button plus an icon-only candle marker bound as
`candle_reward` when entering feeds a hero goal) - then branches to its surface. Both the
prompt and the Field Hospital open synthetically via code and close through their own
teardown (probed live: `ShowEnterNodeScreen((Action)null, key, true)` reads through the
floor and dismisses clean; `ShowHospital(empty list)` opens, closes, and additionally
reveals the player-inventory entry it raises beneath for its Pharmacy tab - one extra
close in a synthetic test). Where each node lands:
- CACHE / CACHE_GANG -> prompt -> the loot screen. Covered, player-verified.
- STORE (the Hoarder) -> prompt -> the shared store surface. Covered, live-verified.
- STORY_ASSIST / RESIST / COSMIC / CULTIST (+ gang/coven variants) -> prompt -> the story
  screen, sometimes into combat and loot. Covered surfaces; story commits mutate the run,
  so exotic variants are left to organic play.
- STORY_HERO (Shrine of Reflection) -> hero story intro + story combat. Covered,
  player-verified. STORY_HERO_REPLACEMENT -> the replacement-hero screen. Covered.
- WATCH_TOWER -> prompt -> `TriggerScouting.StartScoutPulse` only: NO further UI - the
  reveal reads through the road map. Effectively covered.
- HOSPITAL -> prompt -> the Field Hospital, now a dedicated screen (see its own section).
- DUNGEON / GUARDIAN / CREATURE_DEN / kingdom gang bosses -> prompt -> combat chains
  (covered) - not testable-with-undo, entering commits a fight.
- OASIS / GATE / BRIDGE and the kingdoms node skins ride the same trigger set (prompt +
  effect/loot/story/mode triggers); no bespoke screens found in code.
**SYNTHETIC-TEST RULE learned the hard way (2026-07-31): wait for the run to settle
before any synthetic screen push.** The run's managers (`GameTypeMgr.RunValues` and
friends) start a beat AFTER `CurrentMode == DRIVING` flips; a synthetic push inside that
gap stalled the run start for the whole session (dead RunValues, the game's own
UpdateHealCost NRE-ing, "N/A" costs everywhere) - broken until restart, with no log
line. Gate on `RunValues != null`, not just the mode.

### Node-arrival prompt (`EnterNodeScreen`)
Status: **live-verified 2026-07-31** (synthetic opens; a real node arrival is organic)
- The one screen every roadside stop halts on (`EnterNodeScreenWidgetBhv`), named by the
  authored "road stop": a single button reading the interaction's own loc key through the
  push params ("Search the Cache", "The Field Hospital"), with the authored "candle
  reward" value while the game shows its icon-only candle marker (entering feeds a hero
  goal). The push params land a frame after the object tops the stack, so the entry reads
  a bare "button" once and the label's arrival requests the one re-announce (the shared
  LabelArrived pattern). Enter is the button's own press; the game refuses to close the
  prompt, so Escape answers "unavailable".

### Field Hospital (`HospitalScreen`)
Status: **live-verified 2026-07-31 on a real run** (synthetic open, damaged party for
real costs; an actual node visit and a treatment purchase remain organic; the same class
should serve the inn physician - unverified there)
- The road node's `HospitalScreenBhv`, named by its own composed title ("Field Hospital:
  Triage" / Wellness / Pharmacy - it retitles per tab). Layout: the hero pager
  (Left/Right page the party through the browser's own stepping, name + HP + stress,
  status tooltip in the buffer), the tab selector (the tab buttons' own captions; the
  game disables the active tab's button, which is how the current one reads; Left/Right
  click the game's own buttons), then the active tab's rows.
- **Triage**: the cure-disease button (its own texts; "Upgrade Physician to unlock" rides
  the tooltip into the buffer), then minor and full heal - each reading its own amount
  label ("+8 HP", "+MAX") with the price composed from the model exactly as the store
  composes it (CostDescription, strikethrough off - the game's own bound text carries a
  crossed-out original price that would read as two numbers).
- **Wellness**: treatable quirks by their own names ("selected" on the one the commands
  would treat; Enter is the row's own click, spoken back), then the lock/remove commands
  captioned by their tooltips with the cost following ("Remove, relic 16") - the visible
  row is icon plus cost only. The game's "No Treatable Quirks" notice reads when shown.
  Rows are reused per button across the game's per-selection rebuilds so focus never
  re-homes mid-flow.
- **Pharmacy** hands the surface to the shared store screen (the embedded `StoreUiBhv`
  matches it); Escape there returns to Triage through the hospital's own first tab - the
  embedded store's done button is a husk (it only hides the inventory panel and would
  strand an empty store; found live, fixed in the store screen's back). Escape on the
  hospital itself closes through the widget's own close-button handler; the synthetic
  flow also reveals the player-inventory entry beneath (one extra Escape).

### Free driving HUD (`DrivingScreen`, the DRIVING-mode floor)
Status: **live-verified 2026-07-31** (logical paths and binding suppression; physical keys
ride the shared KeyboardBinding path)
- The road HUD as Tab panels around a pass-through driving area, kingdom-map style (Panel
  root, wrapping Tab). Entry announces "driving" then the biome name (the minimap's own
  label; authored "road" when absent). The screen never captures the keyboard.
- **Driving area** (first stop): every key stays the game's - arrows/WASD drive, M, I, C,
  Z, G, Alt, Ctrl, Escape as shipped. The mod claims only Tab (the game's second minimap
  key - its tab binding rests via an empty binding override the whole stand; M keeps the
  map). Arrows are consumed mod-side so focus stays parked.
- **Status panel**: distance ("7 leagues to Inn") and region ("Regions: 1/3") from the
  HUD's own labels, the flame value (authored "Flame {0}" - the game captions it with the
  glyph alone), armor and wheels (the stagecoach sheet's own stat strings over the live
  run values, damage tooltips in the buffer), and the Loathing meter (named by its own
  tooltip, "The Loathing Abates" + confession in the buffer). The flame's buffer IS the
  game's hold-Alt torch panel (Alt only plays that panel's visual intro, so nothing
  keyboard-side is lost): the state name ("Bright Light"), then each side's effects under
  the game's own captions ("Heroes, +6% death RES" / "Enemies, Round Start: blind-line
  (10%)"), read live from the panel's DataContext, which the game re-stamps on every
  torch change. Live-verified 2026-07-31 at flame 88.
- **Party panel**: one element per ribbon hero in marching order - name, HP, stress (the
  game's status-bar strings from the live actor), the shared "New" marker while the
  ribbon shows its notification dot (unviewed sheet notifications, live-verified), every
  ribbon tooltip in the buffer
  (status effects, "Tense", diseases). Enter is the ribbon's own right-click inspect (the
  hero's sheet - verified it opens the FOCUSED hero). Space grabs for a marching-order
  move: place on another hero's slot runs the game's own drag (the hover-index field plus
  OnHeroRibbonDrag/OnHeroRibbonRelease, so locked-ribbon shifts stay the game's), and the
  landing speaks the resulting order. Live-verified both directions and the model commit
  (TeamPositions), then restored.
- **Goals panel** (only while the game's G panel is open; the game toggles, our tree
  follows by signature): the panel is player-summoned, so the moment its rows arrive
  focus jumps to the first row and the panel reads out (live-verified both ways: the
  close re-homes to the driving area; entering the screen with the panel already open
  does not jump). Content: the biome's mutator and goal sections when the biome has them
  (unexercised - the Valley has neither), and one row per hero goal - hero name through
  the game's own row-to-party mapping (the row shows only a portrait), the goal's own
  text with progress count, the reward tooltip in the buffer ("Reward: candle 2").
  GOTCHA: the panel's sections activate a beat after IsBiomePanelActive flips (timeline
  activation), AND row active-states keep flickering through the open timeline - a tree
  built over only-active rows churned rebuilds every flicker (each one orphaned focus and
  re-fired the content-edge jump: "goals, The Valley, goals, ..." - user-caught
  2026-07-31). The swept set therefore includes inactive rows (stable tree, stable
  signature), elements are reused per row, each label answers null while its row is
  hidden, and the focus jump keys to the game's own open flag - once per summon.
- **Buttons panel**: Map (M), Goals & Conditions (G), Inventory (I), Stagecoach (Z) by
  their own tooltip captions, plus the last-chance trophy button (swept inactive, focus
  follows its live state; unexercised - none active this run).
- **Off the driving area** the game's arrow, Space, Enter, and bare-Ctrl bindings rest
  (same override mechanism as the road map - generalized `DrivingKeySuppressor`) so list
  navigation cannot steer, Interact, or flash the glossary; WASD and the letter hotkeys
  stay live everywhere. On-area everything restores (verified in the live input asset,
  both directions). GOTCHA (user-caught 2026-07-31, probe-confirmed): applying or
  removing a binding override re-resolves the whole action state, and the game's
  InputSystem re-fires still-held Button actions in the process - the physical G that
  summoned the goals panel toggled it straight back closed when the focus jump engaged
  the off-area claim mid-press ("the sound twice"; invisible to /input and eval tests,
  which hold no key). Suppressor transitions now wait out any held toggle-class key,
  retried by the per-frame reassert/restore loops; WASD and arrows deliberately do not
  defer (their continuous actions re-fire harmlessly, and cruising holds W). Escape stays the game's pause everywhere; our caption-hotkey handlers
  (M/I/C) stand down whenever the gate is not captured, so shared-keyboard screens never
  double-fire the game's own keys.
- Coexistence live-verified: hero sheet (Enter) in and out, minimap over driving (map
  screen takes over, "map closed" then the driving re-announce), goals toggle in and out,
  the stagecoach sheet and inventory keep outranking via the stack. Combat/inn/etc.
  unaffected (mode-gated).

### Road map (`MapScreen`, M while driving)
Status: **new, deployed 2026-07-25, awaiting live verification**
- The game's minimap overlay, which does not pause the coach - so the screen SHARES the
  keyboard instead of taking it: our arrows walk a map cursor (the game's arrow bindings
  are disabled with empty binding overrides while the map stands, re-asserted per frame
  and restored on close), WASD keeps steering, and M / Z / Escape stay the game's own
  (Escape closes through the game's handler; the mod speaks "map closed" on any close).
- The cursor (STS2-style tree walk over the minimap's own node/link graph): starts at the
  **wagon** - on its node when the coach stands at one ("at Assistance Encounter,
  traveled"), else a synthetic between-nodes position read live ("on the road, Gate to
  Hoarder"), since the coach keeps moving. Up crosses one road per press ("road, node";
  a fork's first alternative is prefixed "choice"); Left/Right swap among that fork's
  alternatives; Down retraces the exact path taken, then the traveled road, then back
  onto the wagon. Home jumps to the wagon, End to the biome's last row. Landings play the
  node-type audio tick. Auto-advance through no-choice stretches was tried and removed;
  it is planned to return as an opt-in setting.
- **Fog of war is enforced by construction**: every node and road name reads through the
  game's own fog-gated tooltips (`MinimapIcon.GetTooltip()` returns the "Unknown" tooltip
  until revealed; roads read the unknown-route tooltip until `IsRevealed()`), and
  unrevealed landings tick as the unknown timbre, never the true type.
- Node line: fog-gated name, then candle/loathing/contract markers (the sighted overlay
  icons, read from their live objects) and traveled/not-taken state. The buffer holds the
  full tooltip, marker tooltips, row position, and one line per road out ("Barricade
  combat, to Lair..."). The wagon's buffer carries its road's route line and row position.
- Known gaps: phase 1 walks the current biome's ladder (biome boundaries hand over only
  where the game links them); reveal events (scouting, watchtowers) are not announced as
  they happen; no points-of-interest jump or user markers yet (the STS2 features staged
  for phase 2); the whole screen is awaiting live verification.

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

### Hero story intro (`HeroStoryIntroScreen`, HERO_STORY_INTRO mode)
Status: **new** - the chapter card shown at a shrine before a hero story resolves; the mode
previously had no screen at all (silent, keyboard released).
- One readout: the chapter title leading (from the game's `hero_story_title` binding, which
  lands after an async portrait load - the entry announce falls back to the hero's name and
  a re-announce fires once the title binds), the hero's name, and the chapter body text as
  buffer lines. The game's own narrator voices the title and, on story chapters, the body.
- The Continue button appears only after the presentation and narration finish (the game's
  fade-in cue); its appearance is spoken. Enter drives the uGUI submit path (the game's own
  click handler); the game gates it with its own input-enabled flag.
- Escape is inert: the game itself blocks the pause menu in this mode, so silence matches
  the sighted experience.

### Confession select (`BossSelectScreen`)
Status: **deployed** (verified on a dev-shown instance at an inn; the real road trigger is
unexercised - it fires once per run, early in the drive)
- The "Complete your Confession" screen a road trigger pushes early in a run: one element
  per confession option (the game's own labels; locked confessions carry the game's "???"
  placeholder, which the text filter already reads as "unknown"), then the confirm button -
  icon-only in the game with no tooltip, captioned here with the game's own `continue_label`
  string. Enter on an option is the game's own submit (marks it, arms the confirm, reads
  back "selected"); confirm commits the confession and the drive resumes.
- Escape is deliberately inert, like a road story: the choice is mandatory. Before this
  screen existed the generic floor took the surface, whose Escape is `TryCloseScreen` - a
  player escaped past the choice, and a run without a confession has no
  `RunManager.Boss`, therefore no Mountain route: the last inn's Select Route screen is
  genuinely empty, embark can never arm, and the game's own forced-run-end detection also
  assumes a boss, so the run dead-ends with End Expedition as the only exit (observed live
  2026-07-24).

### Altar of Hope (`AltarScreen` hub + one screen per panel, ALTAR_OF_HOPE mode)
Status: **works** (live-verified 2026-07-24 on the first-visit intro altar, including
player-driven candle spends; Living City 2026-07-28, purchases unexercised)
- The hub (previously dead air - the altar is a mode surface with no stack entry): the
  candle balance ("Candle of Hope, 5" - the game's own item name over the profile's live
  CANDLES value), the six region markers of the altar map as one list (named by the game's
  `altar_region_<key>_name` strings; a region the game has disabled reads "unavailable" -
  the game locks by disabling the Selectable COMPONENT, which a generic sweep misses - with
  the sub-screen's own unlock requirement in the buffer, the text the sighted lock tooltip
  shows: "Unlock all heroes to gain access to The Mountain"), then **The Recollection**
  (the collection gallery has NO region marker - the sighted path is the panel tab bar, so
  the hub lists it after the regions, opening through the collection's own
  `ToggleSubScreenElement`; hidden on the intro altar like its bar button), then Embark.
  Enter on a region is the game's own submit (opens its sub-screen); Embark drives
  `OnEmbark` with its spend-your-candles-first reminder dialog; Escape opens the pause menu.
- **The Recollection reads as the collection gallery** (`AltarCollectionScreen` over
  `AltarCollectionSubscreenBhv`, live-verified 2026-07-28; always unlocked): a filter tab
  selector first (All Items, Combat Items, Trinkets, ... - Left/Right switch via the
  panel's own `OnInventoryFilterPressed`, rebuilding the list), then every collected item
  as a browse-only row - title, "New" while the game's unviewed marker shows (the game
  clears it for the next visit as it lists the item), full item tooltip in the buffer. The
  game lists items one per frame, so the tree follows the live set by instance-id
  signature, reusing elements so focus holds while rows fill in below.
- The item-unlock panel (`AltarItemScreen` - renamed from AltarRecollectionScreen; the
  game's "The Recollection" is the gallery above, while this panel's progress line merely
  says "Recollection: 9/163") over `AltarItemSubScreenBhv`, "The Working Fields", reads:
  balance, the
  total line ("Recollection: 3/163"), and the unlock-category buttons with progress and
  cost composed from their bindings ("Trinkets, 1/73, 1 candle" - authored plural for the
  cost the game shows as icon+number). Enter purchases in ONE press by driving the game's
  own (private) `Purchase` - the game's gesture is a mouse hold, and a synthetic hold risks
  re-purchasing if the reveal timeline ever skips its pause; the purchase self-validates,
  so a no-op answers "unavailable". Escape closes through the panel's own `CloseSubscreen`
  - closing via a raw `TryCloseScreen` skips the altar's pop flow and leaves every region
  marker disabled (found live; the repair is the game's own `CheckToEnableSubScreenButtons`).
- **The item reveal reads as a modal** (`AltarRevealScreen`, outranking the panel): while a
  purchase presents, the one element speaks "unlocked" then the item's name and full
  description (buffer-reviewable line by line); arrows cannot wander mid-reveal, and Enter
  or Escape continues (the game's own Submit step). The screen matches only once the name
  binding holds THIS reward's name (`item_name_<activeRewardId>`), because the binding lags
  the purchase by an icon load - without the gate the previous reveal re-reads on the next
  purchase (observed live). On return the panel re-announces with focus restored onto the
  purchased category, so its updated count is the landing line and another Enter pulls
  again.
- **The game-options panel reads fully** (`AltarOptionsScreen` over
  `AltarOptionsSubscreenBhv`, "The Dam"): one settings row per altar option, reusing the
  options screen's own row element - the generic floor read this panel as dead air because
  each Toggle is a bare checkmark object with its caption in a sibling label. A row the
  profile has not earned reads its state plus "unavailable" and carries the game's own
  unlock-requirement line in the buffer (the game swaps it into the row's tooltip binding
  on `SetLocked`); an earned row toggles with Enter and reads back its new state. The
  profile saves through the panel's own close (Escape). The option rows spawn a beat after
  the stack entry appears, so the screen follows an instance-id signature and re-announces
  when the first fill lands late - a one-shot build entered on the entry frame stayed
  empty forever (observed live 2026-07-28).
- **The Living City reads as hero rows** (`AltarClassScreen` over
  `AltarClassSubScreenBhv`): the candle balance, then one horizontal row per roster hero -
  the hero's icon button first (name + the track's spent/total binding), then the track's
  milestone diamonds left to right, each named by its reward tooltip's headline with the
  candles still needed ("New Skill, button, 8 candles") or "unlocked" once bought, the
  reward description in the buffer. Up/Down move between heroes announcing the hero as row
  context (the row label dedupes against the icon element on the row head); rows remember
  their column, and Up from the top row climbs out to the candle balance (the navigator's
  vertical spill previously stopped at the first enclosing vertical list, stranding the
  balance above the row block - user-caught). The generic floor previously read this panel
  as a flat list of
  identical-label diamonds with a dead Enter: `ProgressTrackMilestoneBhv` has no submit
  handler - the game sells only through a pointer/Submit-action HOLD gated on the diamond
  being the EventSystem selection. Enter instead drives `AttemptToPurchaseMilestone`
  (buy-up-to-this-milestone, the hold's meaning) behind the same full-affordability check
  the game gates the hold on, answering "unavailable" when short or bought; Enter on the
  hero icon is the game's own `OnIconClick` - one candle banked into the track (or the
  store dialog on an unowned-DLC row), reading back the moved total. A quest-locked hero
  (disabled icon button, no diamonds spawned, tooltips suppressed) reads "unavailable" with
  the game's lock caption in the buffer. The panel spawns rows one per frame on open, so
  the tree follows an instance-id signature, reusing built rows to keep focus. Escape
  closes through `CloseSubscreen` (the same region-marker trap as the recollection panel).
  Purchase paths are code-verified only (candle spends not exercised live).
- **The Intrepid Coast reads as upgrade-track rows** (`AltarGeneralScreen` over
  `AltarGeneralSubScreenBhv`, live-verified 2026-07-28): the candle balance, then one
  horizontal row per stat track (Journey, Resourcefulness, Companionship, Renown, The
  Infernal Flame) - the track's icon button first (name + spent/total; the game binds the
  loc KEY `altar_upgrade_<id>` as the raw context value, so the mod localizes it), then the
  milestone diamonds with reward names, costs, and buffer descriptions, all through the
  shared `AltarTrackElement`/`AltarMilestoneElement`. Enter on the icon banks one candle
  (`OnTrackSpendAttempt`), Enter on a diamond buys up to it; spends not exercised live.
  Escape closes through `CloseSubscreen` (region markers re-enable, verified).
- **The Timeless Wood reads as the memory panel** (`AltarMemoryScreen` over
  `AltarMemorySubScreenBhv`, live-verified 2026-07-28 - hero rows via the game's own
  all-memories dev pref, since the dev profile has no run-survived hero): the candle
  balance, the Memory unlock track as a shared track row (icon + milestones - the track's
  milestones spawn a beat after the stack entry, so the rebuild signature includes them),
  the game's "Heroes with memories are required" notice when it shows, then one horizontal
  row per memoried hero. Each memory slot is keyed to a confession boss and named by the
  game's own `boss_choice_<id>_label` ("I. Denial") - the identity the sighted slot carries
  as a boss sprite - reading "empty" (Enter opens the game's selection list), "unavailable"
  (Enter shows the game's run-locked dialog), or the held memory's item name with its
  tooltip in the buffer and, once earned, "reroll N candles" on Enter (the game's own paid
  reroll). The open selection list swaps the tree modal-style: one element per offer (item
  name + candle cost, tooltip in the buffer), Enter committing through the game's own
  select-and-buy pre-gated on the cost (the game's own failure path would close the whole
  list), Escape closing back to the opened slot, whose re-announce is the dismissal read;
  mid-reroll Escape answers "unavailable" because a paid reroll must pick an offer.
  Unexercised live: an actual memory purchase, the reroll flow, and a natural locked slot
  (all need a run-survived hero on the profile).
- **The Mountain reads as the cosmetic panel** (`AltarCosmeticScreen` +
  `AltarCosmeticRevealScreen` over `AltarCosmeticSubScreenBhv`; browse side live-verified
  2026-07-28 via the game's own unlock-cosmetic-altar dev pref - the profile has it
  legitimately locked): the candle balance, then one reward button per hero ("Man-at-Arms,
  0/6, 3 candles") named by the class string of the button's unlock-track id (the sighted
  button is a bare portrait), through the shared `AltarUnlockButtonElement` (now
  generalized over presenting/resume delegates instead of the item panel type). A
  DLC-locked hero button reads "unavailable" with the game's caption in the buffer and
  Enter raises the game's own store dialog (the game's release handler). The reveal screen
  mirrors the recollection's but needs no name-match gate: the cosmetic panel writes
  item_name/item_desc synchronously when a purchase lands, so presenting plus a non-empty
  description is current by construction. Unexercised live: purchases, the reveal, and the
  DLC-locked read. The hub's milestone pool
  readouts (candle-threshold rewards) were empty on the intro altar and are unread. Embark's press is verified only up to (not
  including) the exit. The reroll variant of the item panel (`m_isRerollScreen`, after full
  completion) shares the class and should read identically but is decades of candles away.

### Patch notes (`PatchNotesScreen`)
Status: **works** (live-verified 2026-07-27; previously the generic floor, which swept only the
Close button and never reached the notes themselves)
- The patch notes overlay (`PatchNotesWidgetBhv`, a Modal-layer stack entry from the main menu
  and PauseModal from pause - both routes verified), named by its own title label.
- Not a text dump: the page's header (the version heading) is the first row and **each note is
  its own row**, arrowed through like any list, so the player can walk the changes at their own
  pace instead of stepping a buffer. Rows read their line live by index off the widget's label;
  a page flip rebuilds them.
- **Left/Right flip pages from anywhere on the screen** (the screen's own `HandleAction`, not
  an element's adjust) through the widget's `TryPreviousPage`/`TryNextPage` - newest page
  first, so Right walks back through history. A page runs to dozens of notes, so paging must
  not mean arrowing back to the top. A flip lands focus on the new page's header, the only
  honest place to leave it (every note below belongs to a page that is now gone); at either
  end the refused flip re-reads the current header. Rich-text style tags are stripped by the
  pipeline.
- The prefab ships placeholder page text and the widget writes the real page during the
  screen's own open step, so the page reads as nothing until `ScreenState == Open` and the
  settle asks for one re-announce - the entry reads "Patch Notes" then the real version only.
  The same gate covers a reopen, which would otherwise read the page left from last time
  before the widget resets to the first (verified: paged deep, closed, reopened, read page 1).
- Escape/Close close through the screen's own `TryCloseScreen`; the main menu re-announces.
- Known gaps: the caption-less prev/next arrow buttons are deliberately out of the tree
  (paging is the screen's); no way to jump to a specific version; the Feedback flow
  (controller-only Submit in the game's own handler) is unexplored; Home/End move within the
  focused list rather than the whole screen, so End from the notes reaches Close but Home from
  them lands on the first note, not the header.

### Token glossary (`TokenGlossaryScreen`)
Status: **deployed, needs live pass** (previously the generic floor: a flat list of
"button"-role rows named after the legend's first label, with no category info)
- The token reference overlay (`TokenGlossaryWidgetBhv`, a stack entry openable over pause,
  combat, the inn, the academic view), named by the pause menu's own "Token Glossary"
  caption. The game shows a flat token list whose name colours encode the category and an
  on-screen legend of colour pips decodes them; the mod reads each category as **one labeled
  horizontal row** (the legend's own caption strings; Left/Right within a row, Up/Down
  between categories) holding its tokens in the game's order. The
  colour-to-caption pairing uses the game's own data both ways: each row's bound
  `name_colour` is the exact hex the game reads from its `glossary_*` colour loc strings,
  matched back to the legend caption keys (`buffs_label`, `glossary_stealth_label`,
  `debuffs_label`, `glossary_other_label`, `glossary_special_label`,
  `glossary_enemy_type_label`). Hero tokens carry special's exact colour and biome tokens
  unique's, so they fold into "Hero & Combo" / "Unique" just as they do visually; a colour
  matching no group reads uncategorized, the same unexplained shade a sighted player sees.
- Rows are plain entries, not buttons: the game wires no click or submit to them (their
  Selectable only anchors the controller highlight), so the old "button" role and dead
  Enter are gone. The token's name is the focus line (glyphs in names speak as words via
  the pipeline); the full description is the buffer. Escape closes through the game's own
  `HideTokenGlossary`.
- The list's contents are the game's own context filter (viewed tokens, combat-contextual
  tokens, the party's hero tokens, the kingdom gang's uniques, biome tokens while
  driving/in combat), so the same screen legitimately lists different tokens per surface.

### Tutorial archive (`TutorialArchiveScreen`)
Status: **deployed, needs live pass** (previously the generic floor: bare title buttons,
no text on activation, no unviewed marker)
- The pause menu's Tutorials screen (`TutorialArchiveWidgetBhv`, also pushed by the game
  with a specific tutorial), named by its own "Archive" title (`tutorial_menu_title`). One
  element per entry in the game's own order (majors, then their category's minors); each
  reads its title (`tutorial_t{eventId}_title`) **prefixed "New"** (authored, translatable)
  while the game shows the entry's unviewed notification icon (the row's bound
  `notification_icon`, live - so the prefix drops the moment an entry is viewed).
- **Enter is the game's own option click** (`OnOptionClicked`): the game opens the entry's
  text in the side panel, marks it viewed, and saves; the mod speaks the landed title and
  full description back (composed from the same `tutorial_t{eventId}_*` loc keys, not the
  frame-late panel TMPs) and the focused entry's buffer carries the description line by
  line while it is the one on display. Escape closes through the screen's own teardown.
- These options **activate on uGUI selection** (`OnSelect` drives `ViewTutorialType` - the
  game's controller path), so browsing must never mirror our focus into the EventSystem:
  scrolling would view-and-clear every entry passed over. Our elements read without
  selecting; only Enter commits.
- Not modeled: the tutorial's image/video (visual-only), the close button (Escape covers
  it).

### Everything else
Status: **not started** (floor-level reading only) - map overlay,
profile select, save management, kingdoms.

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
- The game's "???" placeholder glyph (a locked confession's name on the confession-select
  panel, an unexplored node's "Rewards: ???") is voiced as NOTHING by synthesizers, reading
  as an unlabeled control. The text filter speaks a free-standing run of question marks as
  the authored word "unknown", pipeline-wide; runs attached to a word ("What???" in a bark)
  keep their marks. Live-verified 2026-07-24 on the confession select ("unknown, button,
  unavailable" for the locked entries).
