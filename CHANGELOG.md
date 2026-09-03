# Changelog

## V0.4.0

- On the kingdom map, hero rows now say where each hero is (in the party or the inn they are stationed at), any curse they carry, and where a scheduled trip is taking them.
- Pressing Enter on a stationed hero on the kingdom map now announces the game's hero-travel mode and moves the cursor to that hero's inn; inns in range read as destinations with the route's length in regions.
- The kingdom map's sieges and treasures now say "days" instead of "sun" for their countdown icon.
- The kingdom map's Hero Sheet and Inventory buttons and the cursed-regions counter are now in the map's reading order.
- Closing a hero sheet opened from a kingdom map hero row now lands back on that row instead of the grid cursor.
- A hero sent to another inn now announces their arrival when the day passes.
- The inn's stationed-hero portraits now read name then class, like every other hero row.
- Kingdom map points of interest, borrowed from say-the-spire2: comma and period jump the cursor through a category of cells (inns, camps, sieges, stationed heroes, treasure, regions, cursed and quest regions, the boss, underground access), the brackets switch category and say its count, and backslash narrows every category to what is reachable, from the stagecoach or from the hero you are sending. Shift with the comma and period keys snap you to the top and bottom of the given category.
- Kingdom map cells now say their row and column right after their name, and an arrow pressed at the map's edge is silent instead of repeating the cell.
- Closing the kingdom map no longer announces the inn twice, and opening it no longer re-reads the inn first.
- Kingdom map cells now say how many days of stagecoach travel away they are for the current hero.

## V0.3.9

- An icon standing right beside its own word no longer doubles that word.
- In the tutorial archive, N jumps to the first entry still marked New.
- Rank movement in combat is now announced.
- Scrolling over a combatant now speaks its visible tokens after its HP.
- By popular demand, empty stagecoach slots now say what they are and that they are empty as opposed to just reading item slot.
- Also by popular demand, the game's equipped sheets for e.g., the coach or heroes, now include an explicit element telling you what you're about to equip at the top of the respective screens.
- Pressing Enter on a stagecoach item and then Escape on the Wainwright no longer strands the item reading unavailable.
- Equipping a stagecoach item now parks you on the slot it belongs in, the way hero trinkets and combat items already did.
- Crossroads heroes now read their name ahead of their class, as in combat.
- While driving, heroes now include the rank they're in.

## V0.3.8

- When investing candles one-at-a-time into a hero track, you will now be told progress to unlocking the next item on that track.
- The Strangle debuff inflicted by the Tangle's lair boss now reads by the game's own localized name in every language instead of the raw English "strangle".
- The daze icon in resist lines like "Gain On Stun/Daze/Move Resist" was misread as "Aggressive Stance Immunity", a Duelist token that shares the same icon. It now reads as the game's Daze.
- The Crusader's harvested wheat glyph now reads as a translated "Wheat"; the game only ever draws it.
- New mod setting "crossroads rank sounds" to turn the crossroads and path-seal rank tones off.

## V0.3.7

- Icon words now follow the game language. Currencies (Relics, Mastery, Baubles, Candle of Hope), stat and resist icons (Stun, Move, Disease, Loathing, regen), and the coach's Armor and Wheels used to read as raw English icon ids in every language; they now speak the game's own localized names.
- Resist announcements name the resisted effect in the game language; stress and move resists used to read as raw English ids.
- Item affinity chances now speak the game's own localized "Affinity +" and "Affinity -" words instead of raw English icon ids.
- Store prices paid in quest items (the Crusader quest's Rumour of Riches at the Hoarder) now read the item's localized name instead of its raw English id.
- The rare-quirk star after a quirk's name now reads as a translated "rare" instead of the raw English icon id.
- Inn bonus lines (trophy deliveries, inn tooltips) no longer read a meaningless "animal part" before each bonus; that was the game's decorative bullet glyph.
- Combat item tooltips now read their targeting as the same Rank and Target lines skills use, instead of a string of raw pip glyph names.
- Killing blows now speak their damage before the death line.
- The crossroads now sounds the hero panel's rank circles, which were silent before. A hero in your party plays a tone for the rank they stand in, pitched by how many of their equipped skills work from it (lower for fewer skills), then a four-tone phrase for their reach: how many skills can hit each enemy rank, 1 to 4. A hero in the roster pool, and a path seal. The info for the roster is also available in the buffers.

## V0.3.6

- The mod hotkeys settings page now translates like the other mod pages; its command names were stuck in English no matter the game language.
- Stagecoach liveries earned from Kingdoms campaigns now read by those campaigns' own localized titles instead of raw English ids.

## V0.3.5

- We have some semblance of documentation now!
- At an inn, equipping items directly through hero sheet is now possible. You can now click an empty item slot as opposed to finding the item you want in an inventory first.

## V0.3.4

- You can now hire heroes should one of them die during a run.
- Added a default controller scheme as provided by Ninetales16.

## V0.3.3

- Tinkered with keybinds. They should now work as expected and respect your wish to bind them to multiple actions.
- Hero goals should read slightly cleaner now. We speak their rewards and show them as complete everywhere consistently.
- Skill changes like lost combo speak. The game had no words for this, just tokens disappearing from the visual token strip.

## V0.3.2

- Fixed single trinkets not being able to be removed. Your heroes loved them so much they refused to lose them!
- You can now press `c` while inventory is open to pull up your character sheet. You should have always been able to do this, my bad. Other keys like `m` and `i` also work.
- Equipping a trinket from the inventory now walks you onto the hero's trinket slot to place it, instead of stranding you in a locked inventory where everything reads unavailable.

## V0.3.1

- Added affinity sound as you scroll over a skill that impacts party relations.
- Toasts now speak in every mode, not just combat and the road.
- Token conversions, quirk removals, and dot cleanses now speak, matching the game's own
  pop text.
- The Loathing reset screen now reads its full text instead of one bare button.
- Skill loadouts can now be accessed from the hero sheet.
- Pets now show up as an item within the coach overview.

## V0.3.0

- Biome choices now include all their information like flavor text (Whoops?).
- Tokens that only define a plain name (the Violinist's song-part markers, for instance) no longer leak their raw id into combat speech; they now read by the game's own name.
- Buffs the game names only through a tooltip override no longer read as a raw stat key.
- The deathblow resist icon now speaks as "deathblow" instead of "death"
- Path-modified tokens now read their own descriptions on skill cards.
- Duplicate enemies now number 1 to N in the order you meet them, instead of by rank. I thought the latter would be more intuitive; I was wrong.
- The hero sheet's cosmetics tab now reads properly: palettes, weapon kits, and hero skins by the game's own names with "selected" on the applied one, locked skins refusing with their unlock hint in the buffer.
- The stagecoach's livery cycler now reads as "Stagecoach Livery" with the applied skin's name, and speaks the new skin after cycling. It used to be a bare unlabeled button.
- The hero sheet's conditions tab is no longer a hot mess of one line.
- Opening the hero sheet from the road no longer lands on a bare "hero".
- The crossroads path seals now read their own path's card in the buffer.
- The Infernal Flame Vitrine is now a regular button in the crossroads actions, next to Embark. The game only ever opened it on the Z hotkey, which nothing announced.
- Three run-status glance keys answering from any screen during a run: F speaks the flame level, H the coach's armor and wheels, B the wallet - Relics, Mastery, the Baubles total plus each faction currency you hold by name, and a kingdom's Materials.
- Affinity and party changes outside combat now speak.
- Story choices now tell you who would agree before you commit.
- The road fork is no longer a screen of its own that takes the whole keyboard; it is now a transient popup inside the driving surface, matching the rest of the game. Incidentally, this re-enables looking at things like the map while you're stopped.

## V0.2.9

- Added `a` key to preview skill affinities while you're on one that would cause a change. They should also be present in targetting previews now where applicable. Note that the base game only broadcasts negative changes; positive come as a surprise.
- Remove the announcement of subscreens when they're used as a component of something, e.g., inventory as a part of an inn.
- The subtitle toggle in game now actually does something for us, mainly speak them.

## V0.2.8

- Reviewing the turn order in combat now steps one combatant per line instead of reading the
  whole order back as a single entry.
- Fixed the battlefield going out of sync as a result of trinkets like faceless visage.
- Duplicate enemies now carry their turn-order number everywhere.
- The Shift glance no longer accidentally melds different dots into one entry.
- The inn's floating text now speaks. This includes hero reactions to rest items, refusals and relationship changes.
- Mod management is now fully accessible. I feel like this goes without saying, but I cannot guarantee the game will work as expected when other mods are enabled.
- Arrowing after a Home or End jump now walks from where you landed: in settings, End then
  Home then Down used to teleport back to the bottom row, because the list remembered the
  jumped-from position.

## V0.2.7

- Inn screen path changes now read.

## V0.2.6

- Skills should properly read their upgrades again. Splitting them into separate buffers seemed to have temporarily bricked them.
- When purchasing skills at a Mastery trainer, we no longer say selected and use consistent "Mastered" verbage throughout.
- In combat, your focus shall now land consistently on the round number at the start. It was landing on the combatant strip, and that bothered me.
- We no longer double-announce who you've switched to on the Mastery screen. I know you love Paracelsus, you'll just have to switch back and forth to get her name twice now.
- The weird bug where you'd jump from enemy first rank to the back of your party has been fixed. That's what we get for doing this at 1 in the morning!

## V0.2.5

- The battlefield is now one row laid out like the screen: your party right-to-left (rank 4
  leftmost, rank 1 at the front line), then the enemies rank 1 to 4. Party slots at the crossroads and the driving strip have been fixed accordingly.

## V0.2.4

- Ordained (blessed) enemies now say so
- A new "auto collect pickups" toggle in mod settings (off by default): roadside pickups
  collect themselves as the coach passes them, no steering needed, and the pickup pings stay
  quiet while it is on.
- Road banter finally speaks: hero chatter, relationship exchanges, node-approach barks, and
  the stagecoach pet all read as their bubbles appear. Only scattered reaction quips got
  through before.
- Battle lines no longer leak the game's internal logic markers.

## V0.2.3

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
- The inspector's conditions row now disappears while the hero has no conditions, like the
  token and buff rows already did, instead of reading as an empty stop.
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

## V0.2.2

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

## V0.2.1

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
