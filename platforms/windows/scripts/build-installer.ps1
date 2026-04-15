#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the single uploadable setup EXE: publish\installer\PortKiller-<version>-Setup-x64.exe

.DESCRIPTION
  By default publishes a self-contained win-x64 app (bundles .NET) so the installer works on other PCs
  without installing the .NET runtime. Use -FrameworkDependent for a smaller payload when targets already have .NET 9.

.PARAMETER FrameworkDependent
  If set, publish does not bundle the runtime (smaller; requires .NET 9 on the target machine).
#>
param(
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$csproj = Join-Path $repoRoot "platforms\windows\PortKiller\PortKiller.csproj"
$iss = Join-Path $repoRoot "platforms\windows\installer\PortKiller.iss"
$publishDir = Join-Path $repoRoot "publish\win-x64-installer"

$verLine = Select-String -Path $csproj -Pattern "<Version>" | Select-Object -First 1
if (-not $verLine) { throw "Could not read <Version> from PortKiller.csproj" }
$version = ([regex]::Match($verLine.Line, "<Version>([^<]+)</Version>")).Groups[1].Value

$scArgs = if ($FrameworkDependent) {
    Write-Host "Mode: framework-dependent (target PC must have .NET 9 runtime)"
    @("--self-contained", "false")
} else {
    Write-Host "Mode: self-contained (no .NET install needed on target PC)"
    @("--self-contained", "true")
}

Write-Host "Publishing PortKiller $version (win-x64) -> $publishDir"
dotnet publish $csproj -c Release -r win-x64 @scArgs -o $publishDir

$candidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup 6 not found. Install with: winget install JRSoftware.InnoSetup`nOr: https://jrsoftware.org/isdl.php"
}

$installerDir = Split-Path $iss
$svg = Join-Path $repoRoot "platforms\windows\PortKiller\Assets\AppIcon.svg"
$ico = Join-Path $repoRoot "platforms\windows\PortKiller\Assets\app.ico"
$iconGen = Join-Path $repoRoot "tools\IconGen\IconGen.csproj"
$wizL = Join-Path $installerDir "wizard-large.png"
$wizS = Join-Path $installerDir "wizard-small.png"
$setupIco = Join-Path $installerDir "setup.ico"

Write-Host "Generating setup.ico, wizard images (IconGen)..."
dotnet run --project $iconGen -- $svg $ico $wizL $wizS
Copy-Item -Path $ico -Destination $setupIco -Force

Write-Host "Building installer with Inno Setup..."
& $iscc "/DMyAppVersion=$version" $iss
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler (ISCC) failed with exit code $LASTEXITCODE. Close any running installer with the same name and retry."
}

$setup = Join-Path $repoRoot "publish\installer\PortKiller-$version-Setup-x64.exe"
if (Test-Path $setup) {
    $len = (Get-Item $setup).Length / 1MB
    Write-Host ""
    Write-Host "Upload this file to other PCs:" -ForegroundColor Green
    Write-Host "  $setup"
    Write-Host ("  Size: {0:N1} MB" -f $len)
} else {
    throw "Expected output not found: $setup"
}
