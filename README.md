# Harkest Dungeon

A [Darkest Dungeon II](https://store.steampowered.com/app/1940340/) mod that makes the game
playable by blind users.

## Install

Download `HarkestDungeonInstaller.exe` from the
[latest release](https://github.com/amerikrainian/harkest-dungeon/releases) and run it. It finds
your Steam install, downloads the mod, and can later update, repair, or uninstall it. Manual
alternative: extract the release zip over the game folder.

At launch the mod checks the releases page and announces when a newer version exists ("update
0.3.0 available"); run the installer again to update. Up to date, or offline, it says nothing.

Requires the Steam version of the game on Windows.

## Languages

The mod speaks the language the game is set to, covering every language the game ships.
Switching the language in the game's settings switches the mod's speech immediately, and
the choice carries across relaunches. Game text (menus, tooltips, skills) is read from the
game itself, already localized; the mod's own words come from `lang/<code>.txt` files in the
plugin folder - edit one to adjust a translation (missing entries fall back to English).

## Documentation

The docs can be found [here](http://amerikrainian.com/harkest-dungeon/)

## Building from source

```
dotnet build HarkestDungeon.slnx -c Debug   # builds and deploys into the game's BepInEx folder
dotnet test HarkestDungeon.slnx             # unit tests, no game needed
```

Runs on [BepInEx 5](https://github.com/BepInEx/BepInEx) (vendored); speech via
[Prism](https://github.com/ethindp/prism). The installer is adapted from the
[Non-Visual Calculus](https://github.com/rashadnaqeeb/NonVisualCalculus) installer by Rashad
Naqeeb (MIT).
