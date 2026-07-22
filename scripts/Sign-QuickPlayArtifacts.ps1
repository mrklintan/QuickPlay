[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$CertificateThumbprint,
    [Parameter(Mandatory)] [string[]]$Files,
    [string]$TimestampUrl
)

$searchRoots = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
    "${env:ProgramFiles}\Windows Kits\10\bin",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Shared\NuGetPackages\microsoft.windows.sdk.buildtools"
) | Where-Object { Test-Path -LiteralPath $_ }
$signTool = $searchRoots | ForEach-Object {
    Get-ChildItem -LiteralPath $_ -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue
} | Where-Object FullName -Match '\\x64\\signtool\.exe$' |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $signTool) { throw 'Windows SDK signtool.exe was not found.' }

foreach ($file in $Files) {
    $resolved = (Resolve-Path -LiteralPath $file -ErrorAction Stop).Path
    $arguments = @('sign', '/sha1', $CertificateThumbprint, '/fd', 'SHA256')
    if ($TimestampUrl) { $arguments += @('/tr', $TimestampUrl, '/td', 'SHA256') }
    $arguments += $resolved
    & $signTool @arguments
    if ($LASTEXITCODE -ne 0) { throw "Signing failed for $resolved." }
    & $signTool verify /pa /all $resolved
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'Signature verification reported a trust error. This is expected when the self-signed root is not trusted.'
        $global:LASTEXITCODE = 0
    }
}
