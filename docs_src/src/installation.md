# Installation

The mod requires the Steam version of the game on Windows. It has been verified to work with Gog, but you will need to point the installer to your game folder 

## Installer

The recommended install path is the installer. Download `HarkestDungeonInstaller.exe` from the
[latest releases page](https://github.com/amerikrainian/harkest-dungeon/releases/latest) and run it. It finds your Steam install on its own (registry plus Steam's library folders), downloads the newest release, verifies it, and installs it over the game folder while backing up anything it replaces. It can later update, repair, or uninstall the mod, restoring the folder exactly as it found it.

The default Steam path on Windows is:

```text
C:\Program Files (x86)\Steam\steamapps\common\Darkest Dungeon® II
```

Yes, the folder name genuinely contains a registered-trademark sign. You do not need to type it; the installer deals with the lawyers' handiwork for you.

After installation, launch Darkest Dungeon II through Steam. If installation worked, the mod initializes during boot and begins speaking once the menu is up.

## Manual Installation

1. Download the latest `HarkestDungeon-vX.Y.Z.zip` release.
2. Extract the zip into your Darkest Dungeon II game folder (the folder containing `Darkest Dungeon II.exe`), replacing files when prompted.
3. After extraction, the game folder should contain `winhttp.dll`, `doorstop_config.ini`, and a `BepInEx` folder with the mod under `BepInEx\plugins\HarkestDungeon`.
4. Launch the game through Steam.

## Updating

At launch the mod checks the releases page and announces when a newer version exists ("update 0.4.0 available"). Up to date, or offline, it says nothing. To update, run the installer again and choose update, or extract the latest zip over the game folder.

## Uninstalling

The installer has an uninstall option that restores the game folder from its install records. To remove the mod manually, delete the following from the game folder:

- `winhttp.dll`
- `doorstop_config.ini`
- the `BepInEx` folder (or just `BepInEx\plugins\HarkestDungeon`, if you use BepInEx for other mods)
