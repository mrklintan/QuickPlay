# QuickPlay

<p align="center">
  <img src="src/QuickPlay.WinUI/Assets/QuickPlay.png" width="192" height="192" alt="QuickPlay application icon" />
</p>

<img width="1920" height="1020" alt="image" src="https://github.com/user-attachments/assets/c159db01-786f-4c17-93f7-0d2af7dd236b" />


QuickPlay is a Windows 11 / WinUI 3 desktop application for rapidly auditioning audio tracks. Open or drop a music folder, inspect metadata and a clickable waveform, and move through tracks or sibling folders without interrupting your workflow.

## Purpose

QuickPlay is designed for the moment when you have a large folder of music and need to work through it quickly. Instead of opening every file separately or repeatedly dragging a playback position, QuickPlay starts each track at a configurable audition point and immediately continues from the same point when you move to the next or previous track.

The goal is to make listening, deciding, and organizing feel like one continuous action. You can move through tracks and neighboring folders from the keyboard, click anywhere in the waveform to listen from that position, copy a track you want to keep, or send an unwanted track to the Windows Recycle Bin after confirmation. Metadata such as artist, title, BPM, key, Mixed In Key Energy, and duration stays visible while you review the folder.

QuickPlay is especially useful for DJs, collectors, producers, and anyone sorting downloads, recordings, samples, or large music archives.

## Vibe-coded project

QuickPlay is entirely vibe coded. The application, architecture, interface, playback behavior, waveform processing, shortcut system, documentation, and tests were developed iteratively through natural-language collaboration with OpenAI Codex.

The project is published openly as both a useful audio tool and a practical example of what can be built through an AI-assisted, test-and-refine workflow.

## Features

- Configurable audition start position, defaulting to `01:00`.
- Immediate playback when moving between tracks or sibling folders.
- Clickable waveform seeking and current/total time display.
- Metadata display for artist, title, BPM, musical key, Mixed In Key Energy, and duration, including AIFF/AIF support.
- Optional Disc Number and Track Number columns with multi-disc-aware sorting.
- Playback and waveform analysis for audio files stored at Windows paths that are 260 characters or longer.
- Customizable playlist columns with persistent visibility, order, widths, and sorting.
- Natural, numeric-aware playlist sorting with the current track pinned visually.
- Clear playlist state: unplayed tracks are bold, played tracks are normal, and the active track is italic.
- Independent played-time threshold and optional removal of played tracks.
- Optional Continue Play with a separate start position for automatic track changes.
- Playlist context actions for toggling a track between played and unplayed or marking every track as unplayed.
- The current folder, remaining playlist, and active row are restored after restart without starting playback automatically.
- End-of-folder playback continues with the first supported track in the next sibling folder without wrapping.
- Native File, Settings, and About menus.
- Separate general playback and keyboard shortcut settings dialogs.
- Configurable keyboard shortcuts with conflict confirmation.
- Safe fallback when a track is shorter than the audition position.
- Copy the active file or move it to the Windows Recycle Bin.
- Recursively load supported audio from subfolders while keeping the explicitly opened folder as the sibling-navigation root.
- Open File Explorer with the current audio file selected.
- Clear the current playlist and return to the no-folder-open state after confirmation.

## Required external BASS file

BASS is not included in this repository because it is third-party software with its own licensing terms.

Before building QuickPlay:

1. Download the Windows BASS package from the official Un4seen Developments website: <https://www.un4seen.com/bass.html>
2. Read and accept the licence included in the downloaded package (`bass.txt`). BASS is free for non-commercial use; commercial use requires an appropriate licence.
3. Create this directory inside the repository if it does not exist:

   ```text
   src/Bass/x64
   ```

4. Copy the 64-bit DLL from the downloaded package to:

   ```text
   src/Bass/x64/bass.dll
   ```

The final expected path is `src/Bass/x64/bass.dll`. The `QuickPlay.WinUI` project copies that DLL beside the application executable during build.

At runtime, QuickPlay checks that the 64-bit `bass.dll` can be loaded. If it is missing or incompatible, the application shows a clear message with the official download address and asks whether to open that page.

## Prebuilt Windows x64 release

The GitHub Releases page provides a ready-to-run, self-contained Windows x64 ZIP. It includes the QuickPlay executable, the required .NET and Windows App SDK runtime files, `bass.dll`, and all applicable licence and third-party notices.

Because the prebuilt package includes BASS under its free non-commercial terms, that package is provided for **non-commercial use only**. Commercial use requires an appropriate BASS licence from Un4seen Developments. This restriction applies to the BASS-powered binary package, not to QuickPlay's own source code.

Extract the ZIP to a normal writable folder and run `QuickPlay.WinUI.exe`. Development releases may be signed with a self-signed certificate; Windows can still show a trust or SmartScreen warning because that certificate is not automatically trusted on other computers.

## Build and run

Requirements:

- Windows 10 version 1809 or later; Windows 11 recommended.
- Visual Studio with .NET desktop development and WinUI tooling.
- .NET 8 SDK.
- The external BASS DLL installed as described above.

Open `QuickPlay.sln`, select `QuickPlay.WinUI` as the startup project, choose the `x64` platform, and press F5.

To create the complete self-contained Windows x64 Release used for distribution:

```powershell
dotnet publish .\src\QuickPlay.WinUI\QuickPlay.WinUI.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
```

The executable and all required runtime files are written to:

```text
src\QuickPlay.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish
```

Close QuickPlay before publishing so Windows does not lock the executable. The project publish target automatically includes the generated WinUI `.xbf` and `.pri` resources required at startup.

To build the per-user x64 MSI (including a fresh complete publish):

```powershell
dotnet build .\installer\QuickPlay.Installer\QuickPlay.Installer.wixproj -c Release -p:Platform=x64
```

The installer is written to `installer\QuickPlay.Installer\bin\x64\Release\QuickPlay-1.3.0.0-x64.msi`. Double-click it for the normal interactive setup: review and accept the MIT and third-party terms, install, then close the completion confirmation. It installs without elevation under `%LOCALAPPDATA%\Programs\QuickPlay`, creates a Start Menu shortcut named **QuickPlay**, and supports standard uninstall and future major upgrades.

See [Code signing](docs/CODE_SIGNING.md) for the self-signed development workflow. A self-signed certificate is not automatically trusted on other computers.

To run the behavior checks:

```powershell
dotnet run --project tests\QuickPlay.Tests\QuickPlay.Tests.csproj
```

## Keyboard defaults

- Ctrl+O: open a folder.
- Space: play or pause.
- Up/Down: previous/next unplayed track.
- Ctrl+Left/Right: seek backward/forward by the short duration.
- Left/Right: seek backward/forward by the long duration.
- Ctrl+Up/Down: previous/next sibling folder.
- Ctrl+C: copy the active audio file.
- Delete: confirm and move the active track to the Recycle Bin.

Shortcut assignments can be changed from **Settings → Keyboard**. Audition Start Position, Continue Play, its separate start position, short/long seek durations, **Mark as played after (seconds)**, and the independent **Remove played tracks from playlist** switch are available under **Settings → Settings**. This switch only removes rows from the in-memory playlist; it never deletes audio files. Played time is measured as actual playback time and supports long values such as 600 seconds. If removal is disabled, played tracks remain in the playlist in normal text. If a new shortcut is already in use, QuickPlay asks whether to move it; the previous action then becomes unassigned.

Manual navigation and direct track activation use Audition Start Position. When Continue Play is enabled and a track reaches its natural end, QuickPlay marks it as played and automatically opens the next eligible unplayed track from Continue Play Start Position. If no eligible track remains, playback continues with the next sibling folder without wrapping at the final folder. Disabling Continue Play stops playback at the natural end.

## Playlist layout and playback queue

Artist and Title are always the first two playlist columns. Use **Settings → Playlist Columns...** to add, remove, or reorder optional metadata columns, including Disc Number and Track Number. Drag a column-header divider to resize it, and click a header to toggle ascending or descending sorting. Track Number sorting compares normalized Disc Number first and Track Number second. Values such as `01` and `02/12` are handled safely. Column visibility, order, widths, sort column, and sort direction are saved in the existing QuickPlay settings file.

All newly loaded tracks start in bold text, meaning that they are waiting to be played. The active track stays pinned at the top and is always italic. After the configured playback time it becomes played and changes to normal weight while remaining italic. When moving on, the previous track is appended to the end without re-sorting; if **Remove played tracks from playlist** is enabled and that track is played, its row is removed instead. The audio file remains untouched. Up and Down search in their respective direction for the next bold track and skip normal tracks. A normal track can still be replayed by clicking it directly. Right-click a row to use **Mark as Played** or **Mark as Unplayed**, depending on its current status. **Mark All as Unplayed** is always available. Explicit column sorting pins the active track and sorts the remaining rows.

QuickPlay saves the current folder, active row, and remaining playlist during a normal shutdown. On the next launch it restores the playlist without autoplay. Missing files are reported and a detailed log is written under `%TEMP%\QuickPlay`; if the folder is unavailable or no saved tracks can be loaded, QuickPlay continues with an empty playlist and the default column layout.

## Known issue

After using the application menu, keyboard focus can remain in a state where arrow keys affect both menu/list navigation and playback. Clicking the track list restores the expected playback shortcut behavior. This is tracked in [TODO.md](TODO.md) for a future focus-routing fix.

## Architecture

- `QuickPlay.Core`: settings, track discovery, metadata, and navigation.
- `QuickPlay.Audio`: BASS-backed playback and playback coordination.
- `QuickPlay.Waveform`: background waveform analysis.
- `QuickPlay.WinUI`: Windows input and presentation.
- `QuickPlay.Tests`: dependency-free behavior checks.

Application settings are stored under `%LOCALAPPDATA%\QuickPlay`. On first launch, QuickPlay copies existing settings from `%LOCALAPPDATA%\DJPlayer` when available.

## Licensing

QuickPlay's own source code uses the standard [MIT License](LICENSE). Third-party software is excluded from that licence and retains its own terms; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). In particular, BASS is separately licensed and is not covered by MIT.
