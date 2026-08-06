# Changelog

## v0.2.3

## v0.2.2

- Detail now spreads across purpose-built buffers instead of piling into one. Ctrl+Left/Right
  cycles only the buffers with something to say where you stand: control (the focused
  element, as before), mastery, hero, enemies, party, and combat.
- A skill's upgrade preview is its own mastery buffer instead of trailing the skill card:
  on the hero sheet and the Mastery Trainer, the control buffer reads the card you have,
  Ctrl+Right reads what mastering changes. The buffer takes its name from the game's own
  upgrade header, and an already-mastered skill answers "no upgrade available" rather than
  hiding, so the same keystroke always answers.
- In combat, enemies and party buffers read the whole battlefield from wherever you are:
  one line per combatant in rank order - name, rank, HP, a hero's stress, the pending
  target preview, and the effects summary.
- A story choice with nothing beside the hero's line - no relationship banner, no previews -
  now keeps that line in the control buffer. The buffer's repeat-folding compared details
  against the choice's spoken value, and on such a choice the value was exactly the hero's
  line, so review lost it.
- Home and End now jump to the first and last element of the whole screen - on a road story,
  Home is the first hero and End the inventory button - instead of stopping at the edge of
  the list the cursor is in. Panels stay hard boundaries: where Tab separates panels, Home
  and End keep to the panel you are in.
- Road story choices now speak the choice, not the chooser: "Bigby: Life's luxuries are
  wasted here, party, Relics -12, Flame 30" - the hero's line and every consequence in one
  pass, instead of hero vitals with the actual option buried in the buffer. S reads the
  focused hero's vitals (name, HP, stress) on demand, rebindable like the combat glances;
  the buffer still carries it all line by line.
- Combat glance hotkeys: 1-4 read the enemy strip, Q/W/E/R the party, in rank order, without
  moving your cursor. Bare key is name and health ("Bigby, HP 40/40, Stress 0/10"), Shift is
  the token summary, buffs before debuffs ("Death Armor x2", "Block (3 Turns)"), Ctrl is the
  resistance line ("STUN 20%, BLIGHT 40%..."). Empty slots keep quiet. All rebindable.
- R and Shift+R (rename, reroll) now exist only at the crossroads, where they do something.
  They no longer squat on the keyboard everywhere else announcing "unavailable" - which is
  what freed R to be a hero glance in combat.
- Picking a skill now drops your cursor straight onto the first valid target, preview and
  all, instead of announcing "select target" and leaving you on the skill bar to commute
  from. Arrows browse targets as before; Escape returns you to the skill.
- Three more combat glances: S reads the acting combatant, T on a skill reads everyone it
  could hit right now with hit/crit/damage previews - before you commit to the pick - and
  Shift+T reads the turn order from anywhere. T off the skill bar does nothing, as
  advertised.
- The coach's turning is now audible: a loop while it turns, panned toward the turn and
  louder the harder it leans, and an end cue when it straightens out. Both sit in the mod
  sounds tab with their own volumes.
- The mod sounds tab is no longer one long scroll: after the master volume slider, a tab
  groups the sounds by family (road, nodes, combat), Left/Right to switch.
- The mod now checks for updates after loading and says "update X available" when a newer
  release exists. Up to date, it keeps the news to itself.
- The audio settings tab now reads the active audio device row ("Audio Device, Speakers..."),
  which only OCR could see before: it is plain text with no control, so the sweep skipped it.
- A master volume slider now heads the mod sounds tab, setting the baseline volume of every
  sound the mod plays. The per-sound volumes ride on it.
- Mod volumes now go up to 200 percent, master and per-sound alike, so a quiet sound can be
  boosted past its natural level.

## v0.2.1

- Target picking now says everything the game's attack panel computes: beside hit and crit
  chance, the damage the pick would actually deal with every live modifier folded in (flat
  crit damage once crit is guaranteed), and the target's tested resistances after your
  piercing ("Blight RES 40%") so you know whether the dot will stick.
- Mastery is no longer bought blind: an unmastered skill's buffer now ends with the upgrade
  preview.
- Token descriptions ride at the end of every buffer that mentions a token.
- Thanks to Chaosbringer's report, the very first dialogue when the game pops up reads.
  I really, really should remember to completely nuke saves before handing the mods to him. This happened 2 times in a row now.
- Buffer review no longer repeats chrome: a control's first buffer line is its label and
  state without the role word ("Confessions" instead of "Confessions, button"), and detail
  lines that only restate the name or value (an item tooltip's title, an icon button's
  caption) fold away.
- The mod now speaks in the game's language: translations for all fourteen of the game's
  non-English languages ship with the mod, reusing the game's own terminology.

## V0.2.0

- A mod sounds glossary tab in settings, after the mod settings tab: one row per sound the
  mod plays, named for what it is used for. Enter plays the sound once and Space loops it
  (moving away or closing stops the loop) - both silently, the sound speaking for itself -
  and Left/Right set that sound's volume as a percent of its natural level, saved per sound
  and applied to every future playback of it.
- Roadside pickup pings each play at a slightly different pitch, so several pickups in
  range no longer blend into one sound.
- The mod's own keys are now rebindable from a mod keys tab in settings. Each command holds
  a list of keys: Enter opens its menu to add, replace, or delete one (modifier chords like
  Ctrl+arrows work), a key another command holds is refused by name rather than silently
  stolen, and Shift+Enter restores a command's defaults.
- Controller support on the mod's screens: the dpad navigates, A activates, B backs out,
  the shoulders cross panels, and the right stick reviews buffers. Controller buttons are
  rebindable alongside keys ("add button" in a command's menu while a pad is connected; hold
  a trigger while releasing a button to make a trigger combo), and any controller press
  silences speech in progress.
- Crossroads party slots now say which rank they are ("rank 1, empty slot"), rank 1 being the
  front line as in combat - the four slots used to be indistinguishable.
- The crossroads' hero-path and party-loadout panels now read, so their buttons are finally
  offered: paths list with the full path card in the buffer and a confirm that commits, and
  loadouts list with apply, rename, delete, and save.
- At the crossroads, Enter and Space now do exactly the same thing: pick a hero up, then put
  them down on the slot you press next. Enter used to run the game's own two-step, which left
  invisible state behind and could stop the scene from following you.
- Crossroads heroes can now be renamed where they stand: R renames the hero you are focused
  on and Shift+R rolls them a random name, whether they are in your party or the roster.
  Moving onto a hero also makes the scene show them, so the path panel and the rest of the
  hero controls always mean the hero you are on - they used to act on whichever hero the game
  happened to be showing, which no keyboard move could change.
- The road's sensing range is now a mod setting (mod settings tab, next to the separator):
  type any value from 20 to 200 road units, 80 being the mod's original reach. It governs
  everything the road senses - pickup pings, and node identity ticks at their usual half-
  again reach - and applies immediately.
- The pause menu's Feedback form is now fully usable: the summary and description fields
  edit with keystroke echo and a read-back when the edit ends, the category opens as a menu,
  and Submit reads its unavailable-until-valid state. Escape cancels the report.
- The main menu's mods side is now reachable by keyboard: its Confessions and Kingdoms
  entries read (they lived outside the menu's own selectable list), and Escape backs out of
  an open submenu - the Confessions submenu included, which previously trapped keyboard
  users - before opening settings from the top level.
- The main menu's profile button (the bottom-right journal) now says what it is: it used to
  read as just your profile's name; it now adds the game's own "Change Profile" caption.
- The profile panel behind the main menu's profile button is now fully readable: the
  profile list with rename and delete, creating a profile (name, language, analytics
  consent), and name edits echoing keystrokes and reading back the accepted name.
- Key rebinding now works: the settings controls tab's Bindings button opens the binding
  list (Up/Down pick a command, Left/Right its two key slots), Enter listens for the new
  key and reads back the result, Shift+Enter clears a slot.
- The kingdom map's cell panels (inns, camps, biomes like The Tundra) now read their Close
  button.
- The goals screen, accessible by pressing `g` while driving, has been fixed.
- The driving UI has been mostly if not fully covered. We now support cycling through different UI panels with tab, hero reordering, party flame readouts and its effects, and the stage coach screen. That one actually read fine, but it wasn't quite as pretty.
- The field hospital is now fully supported. No more driving by while having your heroes bleed out to death.
- The Altar of Hope's Intrepid Coast now reads as upgrade-track rows, same layout as the
  Living City: Up/Down pick a stat track, Left/Right walk its milestones, Enter invests or
  buys.
- The Altar of Hope's Timeless Wood now reads fully: the Memory track, then one row per
  hero with a memory slot per confession ("I. Denial, empty" or the held memory with its
  tooltip in the buffer). Enter on an empty slot lists the memory choices with costs; Enter
  buys and assigns, Escape backs out. Rerolling a filled memory is Enter on it once the
  reroll milestone is bought.
- The Altar of Hope's Recollection (the collection gallery of every item you have
  unlocked) is now reachable: it sits after the six regions on the altar list, since the
  game gives it no map marker of its own. Left/Right on its filter tab switch categories;
  each item reads its name, "New" on first viewing, and its full tooltip in the buffer.
- The Altar of Hope's Mountain (cosmetics, once unlocked) now reads: one button per hero
  with unlock progress and cost, Enter pulls a random weapon kit or palette, and the reveal
  reads like the Working Fields one.
- The Dam no longer risks opening empty when its option rows arrive late.
- A locked Altar of Hope region ("The Mountain") now carries its unlock requirement in the
  buffer instead of a bare "unavailable".
- Up from the top hero row of an altar track panel now reaches the candle balance; it used
  to be stuck (the balance was only heard on entry).
- The Altar of Hope's Living City now reads as hero rows: Up/Down pick a hero, Left/Right
  walk that hero's milestone track (each reward named with its candle cost, "unlocked" once
  bought, details in the buffer). Enter on a milestone buys everything up to it in one
  press; Enter on the hero invests one candle at a time.
- Tooltip layout spacer lines no longer land in the buffer as silent presses.
- Dropdowns now open a menu on Enter: Up/Down move through the choices, Enter picks one,
  Escape closes without changing anything. Left/Right no longer change a dropdown in place.
- The main menu's Watch Cinematics panel now reads properly (it used to be unlabeled
  unavailable buttons): the cinematic list and its Back button, with Escape closing the
  panel. While a cinematic plays the keyboard belongs to the game, so any key shows the
  game's skip prompt and holding Space skips.
- Patch notes are now supported, whoops?
- We now have an installer and a release system.

## V0.1.0

Initial release.
