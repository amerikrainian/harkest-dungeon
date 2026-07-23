# Kill + build + launch Darkest Dungeon II with the freshly deployed mod, then wait for the
# dev server. Usage: tools\run-game.ps1 [-NoBuild]
param([switch]$NoBuild)

$ErrorActionPreference = 'Continue'

try { Stop-Process -Name 'Darkest Dungeon II' -Force -ErrorAction Stop; Start-Sleep -Seconds 2 } catch {}

if (-not $NoBuild) {
    dotnet build "$PSScriptRoot\..\DD2A11y.slnx" -c Debug
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

& 'C:\Program Files (x86)\Steam\steam.exe' -applaunch 1940340

$deadline = (Get-Date).AddSeconds(150)
while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri 'http://127.0.0.1:8771/health' -UseBasicParsing -TimeoutSec 2
        if ($r.StatusCode -eq 200) { Write-Host 'dev server up'; exit 0 }
    } catch {}
    Start-Sleep -Seconds 2
}
Write-Host 'timed out waiting for dev server'
exit 1
