// Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
// https://github.com/rashadnaqeeb/NonVisualCalculus

// Library target so unit tests build without the bin's elevation manifest:
// a requireAdministrator test harness cannot be executed by `cargo test`.
pub mod cli;
pub mod core;
pub mod gui;
pub mod i18n;
