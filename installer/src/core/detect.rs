// Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
// https://github.com/rashadnaqeeb/NonVisualCalculus

use std::collections::HashSet;
use std::fs;
use std::path::{Path, PathBuf};

use regex::Regex;

use super::paths::{GAME_ASSEMBLY_MARKER, GAME_EXES, GAME_FOLDERS};

/// Where a detected install came from, recorded in the manifest. The game is
/// sold on Steam only; a new storefront adds a variant here plus its own
/// candidates source in `game_candidates`.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum GameSource {
    Steam,
    Manual,
}

impl GameSource {
    pub fn as_manifest_str(&self) -> &'static str {
        match self {
            GameSource::Steam => "steam",
            GameSource::Manual => "manual",
        }
    }
}

#[derive(Debug, Clone)]
pub struct GameInstall {
    pub path: PathBuf,
    pub source: GameSource,
}

pub fn detect_game() -> Option<GameInstall> {
    let candidates = game_candidates();
    for candidate in candidates {
        if validate_game_dir(&candidate.path) {
            return Some(candidate);
        }
    }
    None
}

pub fn validate_game_dir(path: &Path) -> bool {
    missing_marker(path).is_none()
}

/// The required file `path` lacks, named for diagnostics; None when the
/// directory is a valid game install.
pub fn missing_marker(path: &Path) -> Option<String> {
    if !GAME_EXES.iter().any(|exe| path.join(exe).exists()) {
        return Some(GAME_EXES.join(" / "));
    }
    if !path.join(GAME_ASSEMBLY_MARKER).exists() {
        return Some(GAME_ASSEMBLY_MARKER.to_string());
    }
    None
}

pub fn game_candidates() -> Vec<GameInstall> {
    let mut result = Vec::new();
    let mut seen = HashSet::new();

    // DD2_DIR is the same override the mod's own build uses for the game dir.
    if let Ok(value) = std::env::var("DD2_DIR") {
        add_candidate(
            &mut result,
            &mut seen,
            PathBuf::from(value),
            GameSource::Manual,
        );
    }

    for path in steam_candidates() {
        add_candidate(&mut result, &mut seen, path, GameSource::Steam);
    }

    result
}

fn add_candidate(
    result: &mut Vec<GameInstall>,
    seen: &mut HashSet<String>,
    path: PathBuf,
    source: GameSource,
) {
    let normalized = path.to_string_lossy().trim().trim_matches('"').to_string();
    if normalized.is_empty() {
        return;
    }
    let key = normalized.to_lowercase();
    if seen.insert(key) {
        result.push(GameInstall {
            path: PathBuf::from(normalized),
            source,
        });
    }
}

pub fn parse_steam_library_paths(content: &str) -> Vec<PathBuf> {
    let re = Regex::new(r#""path"\s+"([^"]+)""#).unwrap();
    re.captures_iter(content)
        .map(|cap| PathBuf::from(cap[1].replace("\\\\", "\\")))
        .collect()
}

fn steam_candidates() -> Vec<PathBuf> {
    let mut candidates = Vec::new();

    for steam_root in steam_roots() {
        for folder in GAME_FOLDERS {
            candidates.push(steam_root.join("steamapps").join("common").join(folder));
        }
        let vdf = steam_root.join("steamapps").join("libraryfolders.vdf");
        if let Ok(content) = fs::read_to_string(vdf) {
            for lib in parse_steam_library_paths(&content) {
                for folder in GAME_FOLDERS {
                    candidates.push(lib.join("steamapps").join("common").join(folder));
                }
            }
        }
    }

    candidates
}

fn steam_roots() -> Vec<PathBuf> {
    let mut roots = Vec::new();

    #[cfg(windows)]
    {
        use winreg::RegKey;
        use winreg::enums::{HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE};

        if let Ok(key) = RegKey::predef(HKEY_CURRENT_USER).open_subkey("Software\\Valve\\Steam") {
            if let Ok(path) = key.get_value::<String, _>("SteamPath") {
                roots.push(PathBuf::from(path.replace('/', "\\")));
            }
        }
        if let Ok(key) =
            RegKey::predef(HKEY_LOCAL_MACHINE).open_subkey("SOFTWARE\\WOW6432Node\\Valve\\Steam")
        {
            if let Ok(path) = key.get_value::<String, _>("InstallPath") {
                roots.push(PathBuf::from(path));
            }
        }
    }

    roots.push(PathBuf::from("C:\\Program Files (x86)\\Steam"));
    roots
}

#[cfg(test)]
mod tests {
    use super::*;

    fn write_markers(dir: &Path) {
        fs::write(dir.join("Darkest Dungeon II.exe"), "").unwrap();
        let managed = dir.join("Darkest Dungeon II_Data").join("Managed");
        fs::create_dir_all(&managed).unwrap();
        fs::write(managed.join("IronCrown.dll"), "").unwrap();
    }

    #[test]
    fn parses_steam_library_paths() {
        let content = r#"
        "0" { "path" "C:\\Program Files (x86)\\Steam" }
        "1" { "path" "D:\\SteamLibrary" }
        "#;
        let paths = parse_steam_library_paths(content);
        assert_eq!(paths.len(), 2);
        assert_eq!(paths[1], PathBuf::from("D:\\SteamLibrary"));
    }

    #[test]
    fn validates_game_dir_markers() {
        let dir = tempfile::tempdir().unwrap();
        write_markers(dir.path());
        assert!(validate_game_dir(dir.path()));
    }

    #[test]
    fn rejects_dir_without_game_assembly() {
        let dir = tempfile::tempdir().unwrap();
        fs::write(dir.path().join("Darkest Dungeon II.exe"), "").unwrap();
        assert!(!validate_game_dir(dir.path()));
        assert_eq!(
            missing_marker(dir.path()),
            Some(GAME_ASSEMBLY_MARKER.to_string())
        );
    }

    #[test]
    fn missing_marker_names_the_exe_first() {
        let dir = tempfile::tempdir().unwrap();
        assert_eq!(
            missing_marker(dir.path()),
            Some("Darkest Dungeon II.exe".to_string())
        );
    }
}
