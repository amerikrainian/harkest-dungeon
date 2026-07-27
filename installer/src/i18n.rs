// Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
// https://github.com/rashadnaqeeb/NonVisualCalculus

//! Installer UI strings. The language follows the Windows display language;
//! HARKEST_DUNGEON_INSTALLER_LANG (a tag like "de" or "pt-br") overrides it. The mod
//! ships English only (lang/en.txt); a translation adds a table here and a
//! match arm in `pick`, keyed off the tag.
//!
//! Error details interpolated into {error} come from the core modules and stay
//! English: they are technical diagnostics meant for bug reports.

pub struct Strings {
    pub app_title: &'static str,
    pub cli_header: &'static str,
    pub game_dir_label: &'static str,
    pub browse: &'static str,
    pub btn_install: &'static str,
    pub btn_reinstall: &'static str,
    pub btn_uninstall: &'static str,
    pub btn_close: &'static str,
    pub btn_repair: &'static str,
    pub btn_update: &'static str,
    pub status_detecting: &'static str,
    pub status_not_found: &'static str,
    pub status_ready: &'static str,
    pub status_unmanaged: &'static str,
    pub status_damaged: &'static str,
    pub status_update_available: &'static str,
    pub status_up_to_date: &'static str,
    pub status_invalid_dir: &'static str,
    pub log_connected: &'static str,
    pub log_github_error: &'static str,
    pub log_latest_asset: &'static str,
    pub log_no_asset: &'static str,
    pub log_detected_dir: &'static str,
    pub log_could_not_detect: &'static str,
    pub log_damaged_state: &'static str,
    pub log_invalid_dir: &'static str,
    pub log_game_dir: &'static str,
    pub log_downloading: &'static str,
    pub log_verifying: &'static str,
    pub log_installing: &'static str,
    pub log_whats_new: &'static str,
    pub log_whats_new_version: &'static str,
    pub dir_dialog_title: &'static str,
    pub err_no_zip: &'static str,
    pub err_select_valid: &'static str,
    pub err_close_game_install: &'static str,
    pub err_close_game_uninstall: &'static str,
    pub err_uninstall_managed_only: &'static str,
    pub confirm_uninstall: &'static str,
    pub confirm_uninstall_title: &'static str,
    pub msg_uninstall_complete: &'static str,
    pub msg_uninstall_failed: &'static str,
    pub msg_already_up_to_date: &'static str,
    pub msg_install_complete: &'static str,
    pub msg_first_launch_note: &'static str,
    pub msg_install_failed: &'static str,
    pub cli_no_valid_install: &'static str,
    pub cli_state_not_installed: &'static str,
    pub cli_state_installed: &'static str,
    pub cli_state_unmanaged: &'static str,
    pub cli_state_damaged: &'static str,
    pub cli_menu_install: &'static str,
    pub cli_menu_reinstall: &'static str,
    pub cli_menu_uninstall: &'static str,
    pub cli_menu_exit: &'static str,
    pub cli_choose: &'static str,
    pub cli_invalid_option: &'static str,
    pub cli_use_path: &'static str,
    pub cli_type_path: &'static str,
    pub cli_confirm_uninstall: &'static str,
    pub cli_error: &'static str,
    /// Localized "yes"/"no" keys accepted at CLI prompts (English y/n always works too).
    pub cli_yes_key: &'static str,
    pub cli_no_key: &'static str,
}

pub fn get() -> &'static Strings {
    if let Ok(tag) = std::env::var("HARKEST_DUNGEON_INSTALLER_LANG") {
        return pick(&tag);
    }
    let locale = sys_locale::get_locale().unwrap_or_default();
    pick(&locale)
}

/// Map a BCP-47-ish tag ("de", "pt-BR") to a language table.
pub fn pick(tag: &str) -> &'static Strings {
    let tag = tag.trim().to_lowercase().replace('_', "-");
    let primary = tag.split('-').next().unwrap_or("");
    match primary {
        _ => &EN,
    }
}

/// Fill {name}-style slots. Slots may appear in any order in a translation.
pub fn fill(template: &str, args: &[(&str, &str)]) -> String {
    let mut out = template.to_string();
    for (key, value) in args {
        out = out.replace(&format!("{{{key}}}"), value);
    }
    out
}

pub static EN: Strings = Strings {
    app_title: "Harkest Dungeon Installer",
    cli_header: "=== Harkest Dungeon Installer ===",
    game_dir_label: "Game directory:",
    browse: "Browse...",
    btn_install: "Install",
    btn_reinstall: "Reinstall",
    btn_uninstall: "Uninstall",
    btn_close: "Close",
    btn_repair: "Repair",
    btn_update: "Update",
    status_detecting: "Detecting game directory...",
    status_not_found: "Game directory not found. Browse to select it.",
    status_ready: "Ready to install.",
    status_unmanaged: "Manual install detected. Repair is available.",
    status_damaged: "Installer state is damaged. Repair is available.",
    status_update_available: "Update available. Installed: {version}",
    status_up_to_date: "Up to date. Installed: {version}",
    status_invalid_dir: "Invalid game directory.",
    log_connected: "Connected to GitHub.",
    log_github_error: "Could not check GitHub releases: {error}",
    log_latest_asset: "Latest release file: {name}",
    log_no_asset: "No mod release zip (HarkestDungeon-vX.Y.Z.zip) was found on the latest release.",
    log_detected_dir: "Detected game directory: {path}",
    log_could_not_detect: "Could not auto-detect the game directory.",
    log_damaged_state: "Damaged installer state: {reason}",
    log_invalid_dir: "Invalid game directory: {path} ({missing} not found)",
    log_game_dir: "Game directory: {path}",
    log_downloading: "Downloading {name}...",
    log_verifying: "Verifying download...",
    log_installing: "Installing...",
    log_whats_new: "What's new since version {version}:",
    log_whats_new_version: "Version {version}:",
    dir_dialog_title: "Select the Darkest Dungeon II game directory",
    err_no_zip: "No downloadable release zip was found.",
    err_select_valid: "Select a valid Darkest Dungeon II game directory.",
    err_close_game_install: "Close Darkest Dungeon II before installing.",
    err_close_game_uninstall: "Close Darkest Dungeon II before uninstalling.",
    err_uninstall_managed_only: "Uninstall is only available for installs managed by this installer.",
    confirm_uninstall: "Remove Harkest Dungeon from this game directory?",
    confirm_uninstall_title: "Confirm Uninstall",
    msg_uninstall_complete: "Uninstall complete.",
    msg_uninstall_failed: "Uninstall failed:\n{error}",
    msg_already_up_to_date: "Already up to date. Reinstall can repair damaged files.",
    msg_install_complete: "Install complete.",
    msg_first_launch_note: "Launch the game through Steam. The mod starts speaking at the main menu; the boot there takes about 20 seconds.",
    msg_install_failed: "Install failed:\n{error}",
    cli_no_valid_install: "No valid Darkest Dungeon II install selected.",
    cli_state_not_installed: "State: not installed",
    cli_state_installed: "State: installed, version {version}",
    cli_state_unmanaged: "State: existing manual/unmanaged install detected",
    cli_state_damaged: "State: installer state is damaged ({reason})",
    cli_menu_install: "1. Install / Update / Repair latest",
    cli_menu_reinstall: "2. Reinstall latest",
    cli_menu_uninstall: "3. Uninstall",
    cli_menu_exit: "4. Exit",
    cli_choose: "Choose an option: ",
    cli_invalid_option: "Invalid option.",
    cli_use_path: "Use this path? (Y/n): ",
    cli_type_path: "Type the game directory path: ",
    cli_confirm_uninstall: "Remove Harkest Dungeon? (y/N): ",
    cli_error: "Error: {error}",
    cli_yes_key: "y",
    cli_no_key: "n",
};
