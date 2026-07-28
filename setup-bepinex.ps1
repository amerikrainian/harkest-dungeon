# setup-bepinex.ps1 - Install the vendored BepInEx (third_party/bepinex) into the
# Darkest Dungeon II folder. Idempotent: safe to re-run (e.g. after a game update
# wipes it). The game is Mono, so no interop generation follows: after this, run
# build.ps1 (or dotnet build) to deploy the mod, then launch through Steam.
#
# Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
# https://github.com/rashadnaqeeb/NonVisualCalculus

$ErrorActionPreference = "Stop"

$GameExe = "Darkest Dungeon II.exe"
# Steam names the install folder with the registered-trademark sign; composed from
# its code point so this script survives any text encoding.
$GameFolders = @("Darkest Dungeon$([char]0x00AE) II", "Darkest Dungeon II")

# --- Locate the game install ---
# DD2_DIR env var wins (the same override the mod's own build uses); otherwise
# auto-detect from Steam library folders; otherwise fall back to the default location.
$Game = $env:DD2_DIR
if (-not $Game) {
    $RegSteam = (Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam" -Name InstallPath -ErrorAction SilentlyContinue).InstallPath
    $DefaultSteam = if ($RegSteam) { $RegSteam } else { "C:\Program Files (x86)\Steam" }
    $SteamPaths = @()
    if (Test-Path "$DefaultSteam\steamapps") { $SteamPaths += $DefaultSteam }
    $LibFolders = "$DefaultSteam\steamapps\libraryfolders.vdf"
    if (Test-Path $LibFolders) {
        $content = Get-Content $LibFolders -Raw
        [regex]::Matches($content, '"path"\s+"([^"]+)"') | ForEach-Object {
            $p = $_.Groups[1].Value -replace '\\\\', '\'
            if ($p -ne $DefaultSteam -and (Test-Path "$p\steamapps")) { $SteamPaths += $p }
        }
    }
    foreach ($steam in $SteamPaths) {
        foreach ($folder in $GameFolders) {
            $candidate = "$steam\steamapps\common\$folder"
            if (Test-Path "$candidate\$GameExe") { $Game = $candidate; break }
        }
        if ($Game) { break }
    }
    if (-not $Game) { $Game = "C:\Program Files (x86)\Steam\steamapps\common\$($GameFolders[0])" }
}
if (-not (Test-Path "$Game\$GameExe")) {
    Write-Host "ERROR: Darkest Dungeon II not found at: $Game" -ForegroundColor Red
    Write-Host "Set the DD2_DIR environment variable to the game folder." -ForegroundColor Red
    exit 1
}

# The vendored loader is an extracted game-folder layout: BepInEx\, winhttp.dll,
# doorstop_config.ini (changelog.txt is BepInEx's own release notes and stays here).
$Vendored = "$PSScriptRoot\third_party\bepinex"
if (-not (Test-Path "$Vendored\BepInEx\core")) {
    Write-Host "ERROR: vendored BepInEx not found at $Vendored" -ForegroundColor Red
    exit 1
}

Write-Host "Installing BepInEx into $Game ..." -ForegroundColor Cyan

Copy-Item -Path "$Vendored\BepInEx" -Destination $Game -Recurse -Force
Copy-Item -LiteralPath "$Vendored\winhttp.dll" -Destination $Game -Force
Copy-Item -LiteralPath "$Vendored\doorstop_config.ini" -Destination $Game -Force

Write-Host ""
Write-Host "BepInEx installed. Run build.ps1 (or dotnet build) to deploy the mod," -ForegroundColor Cyan
Write-Host "then launch the game through Steam." -ForegroundColor Cyan
