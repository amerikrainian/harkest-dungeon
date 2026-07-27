// Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
// https://github.com/rashadnaqeeb/NonVisualCalculus

use std::path::{Path, PathBuf};

// The full release list, newest first: one call serves both the latest-version
// lookup and the per-version release notes shown after an update.
pub const GITHUB_RELEASES_URL: &str =
    "https://api.github.com/repos/amerikrainian/harkest-dungeon/releases?per_page=100";
pub const MOD_ZIP_PREFIX: &str = "HarkestDungeon-v";
pub const MOD_ZIP_SUFFIX: &str = ".zip";
pub const GAME_EXES: &[&str] = &["Darkest Dungeon II.exe"];
// Steam names the install folder with the registered-trademark sign; the plain
// spelling covers a hand-moved copy.
pub const GAME_FOLDERS: &[&str] = &["Darkest Dungeon\u{ae} II", "Darkest Dungeon II"];
// The game is Mono: IronCrown.dll is the game-code assembly, in the managed dir
// of every install, so together with the exe it identifies the game dir.
pub const GAME_ASSEMBLY_MARKER: &str = "Darkest Dungeon II_Data/Managed/IronCrown.dll";
pub const PLUGIN_REL: &str = "BepInEx/plugins/DD2A11y/DD2A11y.dll";
pub const MANIFEST_REL: &str = "BepInEx/config/DD2A11y/install.json";
pub const BACKUPS_REL: &str = "BepInEx/config/DD2A11y/backups";

pub fn manifest_path(game_dir: &Path) -> PathBuf {
    game_dir.join(MANIFEST_REL)
}

pub fn normalize_rel(path: &str) -> String {
    path.replace('\\', "/").trim_start_matches("./").to_string()
}

pub fn required_loader_files() -> &'static [&'static str] {
    &[
        "winhttp.dll",
        "doorstop_config.ini",
        "BepInEx/core/BepInEx.dll",
        PLUGIN_REL,
    ]
}
