# Harkest Dungeon

A [Darkest Dungeon II](https://store.steampowered.com/app/1940340/) mod that makes the game
playable by blind users.

## Install

Download `HarkestDungeonInstaller.exe` from the
[latest release](https://github.com/amerikrainian/harkest-dungeon/releases) and run it. It finds
your Steam install, downloads the mod, and can later update, repair, or uninstall it. Manual
alternative: extract the release zip over the game folder.

Requires the Steam version of the game on Windows.

## Languages

The mod speaks the language the game is set to, covering every language the game ships.
Switching the language in the game's settings switches the mod's speech immediately, and
the choice carries across relaunches. Game text (menus, tooltips, skills) is read from the
game itself, already localized; the mod's own words come from `lang/<code>.txt` files in the
plugin folder - edit one to adjust a translation (missing entries fall back to English).

## Keys

**The game's own keys are unchanged.** Everything below is what the mod adds on the screens it
reads; every shortcut the game itself supports (WASD driving, G for goals, C for the hero sheet,
M, I, and so on) still works. On screens the mod captures, it forwards those advertised hotkeys
to the game's own buttons for you.

Navigation:

- **Arrows** - move focus. On a slider, stepper, or tab bar, Left/Right change the value.
- **Tab / Shift+Tab** - next / previous panel.
- **Enter** - activate.
- **Escape** - back / close.
- **Home / End** - first / last item.

Review buffers (tooltips and details of the focused element):

- **Ctrl+Right / Ctrl+Left** - next / previous buffer.
- **Ctrl+Up / Ctrl+Down** - next / previous line.

Items and heroes:

- **Space** - grab the focused hero or item stack; press again on a destination to place it.
- **Shift+Space** - place a single item off a grabbed stack.
- **Shift+Enter** - discard the focused item (the game's shift-click).

At the crossroads:

- **R** - rename the focused hero (type the name, Enter to accept).
- **Shift+R** - roll the focused hero a random name.

In combat, glances speak without moving your cursor:

- **1-4** - the enemy in that rank: name and health. **Q/W/E/R** - the same for your party.
- **Shift+number/letter** - that combatant's tokens, buffs, and debuffs.
- **Ctrl+number/letter** - that combatant's resistances.
- **S** - the acting combatant's status.
- **T** on a focused skill - everyone that skill could hit right now, with hit, crit, and
  damage previews; on an unusable skill, the reason it is grey.
- **Shift+T** - the turn order.
- **I** - open or close the inspector (the game's hold-Alt combatant dossier) on the focused
  combatant; **A / D** cycle combatants while it is open.

Picking a skill with Enter drops the cursor on its first valid target; arrows browse the rest
(a high or low beep marks valid and invalid targets), Enter executes, Escape returns to the
skill bar.

Every mod key can be rebound from the settings screen's "mod keys" tab, keyboard and
controller alike.

One exception while the road map is open: the arrow keys browse the map and Ctrl belongs to
buffer review, so the game's hold-Ctrl token glossary is unavailable there. WASD keeps driving
the coach the whole time.

## Building from source

```
dotnet build HarkestDungeon.slnx -c Debug   # builds and deploys into the game's BepInEx folder
dotnet test HarkestDungeon.slnx             # unit tests, no game needed
```

Runs on [BepInEx 5](https://github.com/BepInEx/BepInEx) (vendored); speech via
[Prism](https://github.com/ethindp/prism). The installer is adapted from the
[Non-Visual Calculus](https://github.com/rashadnaqeeb/NonVisualCalculus) installer by Rashad
Naqeeb (MIT).
