# Build the distributable mod zip: the vendored BepInEx game-folder layout plus the
# Release plugin output under BepInEx\plugins\HarkestDungeon (plugin + Core + NAudio
# dlls, prism.dll, Mono.CSharp.dll, lang files, audio assets) plus the rendered mdbook
# manual under HarkestDungeonDocs. The zip root IS the game folder, so the installer
# (and a manual user) extracts it straight into the game dir.
#
# Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
# https://github.com/rashadnaqeeb/NonVisualCalculus

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$propsPath = Join-Path $scriptDir "Directory.Build.props"
$releaseDir = Join-Path $scriptDir "releases"
$stageDir = Join-Path $scriptDir "obj\release-stage"

[xml]$props = Get-Content $propsPath
$versionNode = $props.SelectSingleNode("/Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Could not read Version from $propsPath"
}
$version = $versionNode.InnerText.Trim()

# The vendored loader is an extracted game-folder layout (BepInEx\, winhttp.dll,
# doorstop_config.ini); changelog.txt is BepInEx's own release notes and is not shipped.
$bepinexDir = Join-Path $scriptDir "third_party\bepinex"
$prismDll = Join-Path $scriptDir "third_party\prism\prism.dll"
$monoCSharpDll = Join-Path $scriptDir "third_party\mono.csharp\Mono.CSharp.dll"
$hostOutDir = Join-Path $scriptDir "src\HarkestDungeon\bin\Release\net472"
$zipPath = Join-Path $releaseDir "HarkestDungeon-v$version.zip"

foreach ($required in @((Join-Path $bepinexDir "BepInEx\core"), (Join-Path $bepinexDir "winhttp.dll"),
        (Join-Path $bepinexDir "doorstop_config.ini"), $prismDll, $monoCSharpDll)) {
    if (-not (Test-Path $required)) {
        throw "Required file not found: $required"
    }
}

Push-Location $scriptDir
try {
    # The plugin dlls are picked from the output dir by pattern, so a stale DLL there
    # (a renamed or removed assembly dotnet build no longer owns) would ship. Start it empty.
    if (Test-Path $hostOutDir) {
        Remove-Item -LiteralPath $hostOutDir -Recurse -Force
    }

    dotnet build HarkestDungeon.slnx -c Release -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE"
    }

    mdbook build docs_src
    if ($LASTEXITCODE -ne 0) {
        throw "Docs build failed with exit code $LASTEXITCODE"
    }

    $pluginDll = Join-Path $hostOutDir "HarkestDungeon.dll"
    $coreDll = Join-Path $hostOutDir "HarkestDungeon.Core.dll"
    foreach ($required in @($pluginDll, $coreDll)) {
        if (-not (Test-Path $required)) {
            throw "Release build output not found: $required"
        }
    }

    if (Test-Path $stageDir) {
        Remove-Item -LiteralPath $stageDir -Recurse -Force
    }
    New-Item -ItemType Directory -Force $stageDir | Out-Null
    New-Item -ItemType Directory -Force $releaseDir | Out-Null

    Copy-Item -Path (Join-Path $bepinexDir "BepInEx") -Destination $stageDir -Recurse
    Copy-Item -LiteralPath (Join-Path $bepinexDir "winhttp.dll") -Destination $stageDir
    Copy-Item -LiteralPath (Join-Path $bepinexDir "doorstop_config.ini") -Destination $stageDir

    # Packaging pattern adapted from SayTheSpire2:
    # https://github.com/bradjrenshaw/say-the-spire2
    Copy-Item -Path (Join-Path $scriptDir "docs_src\book") -Destination (Join-Path $stageDir "HarkestDungeonDocs") -Recurse

    # The same file set the Debug post-build target deploys.
    $pluginDir = Join-Path $stageDir "BepInEx\plugins\HarkestDungeon"
    New-Item -ItemType Directory -Force $pluginDir | Out-Null
    Copy-Item -LiteralPath $pluginDll -Destination $pluginDir
    Copy-Item -LiteralPath $coreDll -Destination $pluginDir
    Copy-Item -Path (Join-Path $hostOutDir "NAudio*.dll") -Destination $pluginDir
    Copy-Item -LiteralPath $prismDll -Destination $pluginDir
    Copy-Item -LiteralPath $monoCSharpDll -Destination $pluginDir

    New-Item -ItemType Directory -Force (Join-Path $pluginDir "assets") | Out-Null
    Copy-Item -Path (Join-Path $scriptDir "assets\audio") -Destination (Join-Path $pluginDir "assets") -Recurse

    $langDir = Join-Path $pluginDir "lang"
    New-Item -ItemType Directory -Force $langDir | Out-Null
    Copy-Item -Path (Join-Path $scriptDir "lang\*.txt") -Destination $langDir

    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -Force

    Remove-Item -LiteralPath $stageDir -Recurse -Force

    Write-Host "Release zip: $zipPath"
}
finally {
    Pop-Location
}
