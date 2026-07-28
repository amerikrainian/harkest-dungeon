# build.ps1 - Build Harkest Dungeon and deploy it into the game. Deploy itself is the
# HarkestDungeon project's Debug post-build target (plugin + Core + NAudio + prism.dll +
# Mono.CSharp.dll + lang + audio assets into BepInEx\plugins\HarkestDungeon); this script
# locates the game, checks BepInEx is set up, and runs the build. Close the game
# first, or the dll copy is skipped (file locked) and you'll run a stale build.
#
# Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
# https://github.com/rashadnaqeeb/NonVisualCalculus

param(
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\build.ps1 [-Help]"
    Write-Host "  Builds the solution and deploys the mod into the game folder."
    Write-Host "  Run setup-bepinex.ps1 once first."
    exit 0
}

$ErrorActionPreference = "Stop"

$GameExe = "Darkest Dungeon II.exe"
# Steam names the install folder with the registered-trademark sign; composed from
# its code point so this script survives any text encoding.
$GameFolders = @("Darkest Dungeon$([char]0x00AE) II", "Darkest Dungeon II")

# --- Locate the game install (same resolution as setup-bepinex.ps1) ---
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
$env:DD2_DIR = $Game

if (-not (Test-Path "$Game\BepInEx\core\BepInEx.dll")) {
    Write-Host "ERROR: BepInEx is not installed at $Game\BepInEx." -ForegroundColor Red
    Write-Host "Run setup-bepinex.ps1 first." -ForegroundColor Red
    exit 1
}

# --- Build (the Debug post-build target deploys) ---
Write-Host "Building Harkest Dungeon (game: $Game)..." -ForegroundColor Cyan
dotnet build "$PSScriptRoot\HarkestDungeon.slnx" -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Done. Launch Darkest Dungeon II through Steam and listen for the startup line." -ForegroundColor Cyan
