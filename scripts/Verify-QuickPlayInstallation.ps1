[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
[xml]$versionProps = Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props')
$expectedVersion = [string]$versionProps.Project.PropertyGroup.QuickPlayReleaseVersion
if ([string]::IsNullOrWhiteSpace($expectedVersion)) {
    throw 'QuickPlayReleaseVersion is missing from Directory.Build.props.'
}

$uninstallRoots = @(
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall',
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall'
)
$products = foreach ($root in $uninstallRoots) {
    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    foreach ($key in Get-ChildItem -LiteralPath $root) {
        $product = Get-ItemProperty -LiteralPath $key.PSPath
        if ($product.DisplayName -eq 'QuickPlay') {
            [pscustomobject]@{
                ProductCode = $key.PSChildName
                Version = [string]$product.DisplayVersion
            }
        }
    }
}

if (@($products).Count -ne 1) {
    $found = @($products | ForEach-Object { "$($_.ProductCode) $($_.Version)" }) -join ', '
    throw "Expected one installed QuickPlay product, found $(@($products).Count): $found"
}
if ($products[0].Version -ne $expectedVersion) {
    throw "Installed MSI version $($products[0].Version) does not match $expectedVersion."
}

$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\QuickPlay'
$applicationFiles = @(
    'QuickPlay.WinUI.exe',
    'QuickPlay.Core.dll',
    'QuickPlay.Audio.dll',
    'QuickPlay.Waveform.dll'
)

foreach ($name in $applicationFiles) {
    $path = Join-Path $installDirectory $name
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required installed file is missing: $path"
    }

    $actualVersion = (Get-Item -LiteralPath $path).VersionInfo.FileVersion
    if ($actualVersion -ne $expectedVersion) {
        throw "$name has file version $actualVersion; expected $expectedVersion."
    }
}

Write-Host "PASS: one QuickPlay $expectedVersion product is installed and every QuickPlay assembly is current."
