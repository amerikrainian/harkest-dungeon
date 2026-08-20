# Troubleshooting And Known Issues

## No Speech

If the mod does not speak:

1. Confirm the release was installed into the Darkest Dungeon II game folder (the one containing `Darkest Dungeon II.exe`).
2. Confirm `winhttp.dll`, `doorstop_config.ini`, and `BepInEx` sit next to the exe, and the mod is at `BepInEx\plugins\HarkestDungeon`.
3. Launch through Steam - the game insists on it, and so must we.
4. Check the BepInEx log:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Darkest Dungeon® II\BepInEx\LogOutput.log
   ```

5. Look for `Harkest Dungeon <version> loaded.`

The log is truncated on every launch, so whatever is in it is from the current session.

## Controller Notes

The mod author lacks a controller, so the default scheme was kindly suggested by a community member and is still being rebound.

## Other Mods

Mod management screens are fully accessible, but the mod cannot vouch for what other mods do to the game's UI. If a screen stops reading after enabling something from the workshop, suspect the something first.
