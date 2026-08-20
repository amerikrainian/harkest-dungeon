# Getting Started

After installing the mod, launch Darkest Dungeon II through Steam. The boot takes a little while; once the main menu is up, the mod announces it and starts reading focus.

## Basic Navigation

Use the arrow keys or the D-pad to move through screens the mod reads. Enter or the controller's A button activates the focused item. Escape or B backs out. Tab and Shift+Tab (the shoulders on a pad) jump between panels on screens that have several, and Home/End jump to the ends of a list.

On a slider, stepper, or tab bar, Left and Right change the value in place rather than moving focus.

**The game's own keys are unchanged.** Everything the mod adds sits on top; shortcuts the game itself supports (WASD driving, C for the hero sheet, M, I, and so on) still work, and on screens the mod captures it forwards those advertised hotkeys to the game's own buttons for you.

## Buffers

Focus announcements are deliberately terse - a name, a role, a value. Everything longer that a control carries (tooltips, stat blocks, skill text) goes into review buffers you read on demand with Ctrl plus the arrow keys, or the right stick on a pad. See [Buffers](buffers.md).

## Mod Settings

The mod adds four tabs to the game's own Settings screen, right after the game's tabs:

- **Mod settings** - the announcement separator, the road pickup sensing range, and auto collect.
- **Mod announcements** - toggles for optional announcements (currently, corpse deaths in combat).
- **Mod sounds** - master volume for the mod's own audio cues plus a per-cue offset, and a glossary that plays each cue so you can learn what they mean before the road teaches you the hard way.
- **Mod keys** - every mod keybinding, keyboard and controller alike. See [Controls](controls.md).

## Languages

The mod speaks the language the game is set to. Switching the language in the game's settings switches the mod's speech immediately, and the choice carries across relaunches. Game text is read from the game itself, already localized; the mod's own words come from `lang/<code>.txt` files in the plugin folder - edit one to adjust a translation, and missing entries fall back to English.
