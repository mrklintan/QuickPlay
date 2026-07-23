# QuickPlay release versioning

`QuickPlayReleaseVersion` in `Directory.Build.props` is the only release-version
source. It supplies:

- the application version;
- `AssemblyVersion` and `FileVersion` for every QuickPlay assembly;
- the MSI `ProductVersion` and output file name.

Do not add project-local version properties. Before every public release, increase
one of the first three version fields. Windows Installer uses the first three
fields for upgrade ordering, so changing only the fourth field is not a reliable
public MSI upgrade.

The MSI performs a major upgrade with a stable `UpgradeCode`. Related QuickPlay
products are removed inside the installation transaction before new files are
copied. Same-version packages are also treated as upgrades. This makes a rebuilt
MSI replace every application file instead of retaining an equal-version EXE or
DLL from an earlier package.

## Release verification

1. Build the complete x64 Release solution:

   ```powershell
   dotnet build .\QuickPlay.sln -c Release -p:Platform=x64
   ```

2. Install the new MSI over the previously released version using the normal
   interactive installer.
3. Run:

   ```powershell
   .\scripts\Verify-QuickPlayInstallation.ps1
   ```

4. Confirm that only one QuickPlay uninstall entry remains and that the EXE,
   Core, Audio, and Waveform file versions all match `QuickPlayReleaseVersion`.
