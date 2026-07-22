[CmdletBinding()]
param(
    [string]$Subject = 'CN=QuickPlay Development',
    [int]$ValidYears = 3
)

$certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -KeyAlgorithm RSA `
    -KeyLength 3072 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears($ValidYears)

Write-Host 'Created QuickPlay development code-signing certificate.'
Write-Host "Thumbprint: $($certificate.Thumbprint)"
Write-Host 'The private key remains in Cert:\CurrentUser\My. Do not commit exported PFX files or passwords.'
