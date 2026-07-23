# Code signing

QuickPlay currently supports a local self-signed development workflow. A self-signed certificate proves that two files were signed by the same local key, but it is not automatically trusted on other Windows computers and does not remove SmartScreen warnings. Replace the certificate thumbprint with a trusted code-signing certificate later; the build and signing commands remain the same.

## Create a local certificate

```powershell
.\scripts\New-QuickPlaySigningCertificate.ps1
```

The script creates an exportable code-signing certificate in `Cert:\CurrentUser\My` and prints its thumbprint. The private key stays in the current user's certificate store. If a backup is needed, export it manually to a protected location outside the repository and never commit the PFX or its password.

## Publish, sign, and package

```powershell
dotnet publish .\src\QuickPlay.WinUI\QuickPlay.WinUI.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
.\scripts\Sign-QuickPlayArtifacts.ps1 -CertificateThumbprint '<thumbprint>' -Files '.\src\QuickPlay.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\QuickPlay.WinUI.exe'
dotnet build .\installer\QuickPlay.Installer\QuickPlay.Installer.wixproj -c Release -p:Platform=x64 -p:SkipQuickPlayPublish=true
.\scripts\Sign-QuickPlayArtifacts.ps1 -CertificateThumbprint '<thumbprint>' -Files '.\installer\QuickPlay.Installer\bin\x64\Release\en-US\QuickPlay-1.3.3.0-x64.msi'
```

Signing the executable before building the MSI ensures that the packaged executable is signed. Signing the MSI afterward signs the installer container.

## Verify

```powershell
Get-AuthenticodeSignature '.\src\QuickPlay.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\QuickPlay.WinUI.exe' | Format-List
Get-AuthenticodeSignature '.\installer\QuickPlay.Installer\bin\x64\Release\en-US\QuickPlay-1.3.3.0-x64.msi' | Format-List
```

The signing script also runs Windows SDK `signtool verify`. A self-signed signature can report an untrusted root on another computer even when its cryptographic integrity is valid. Trust the public certificate only on controlled test systems; do not distribute or trust the private key.
