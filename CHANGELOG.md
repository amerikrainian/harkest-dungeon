# Changelog

## V0.2.0

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
