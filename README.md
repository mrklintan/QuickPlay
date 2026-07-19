# QuickPlay

<img width="1920" height="1020" alt="image" src="https://github.com/user-attachments/assets/6d6f6b20-13a8-4a59-b266-a1ef5ca6e19e" />

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
- End-of-folder playback continues with the first supported track in the next sibling folder without wrapping.
- Native File, Settings, and About menus.
- Separate general playback and keyboard shortcut settings dialogs.
- Configurable keyboard shortcuts with conflict confirmation.
- Safe fallback when a track is shorter than the audition position.
- Copy the active file or move it to the Windows Recycle Bin.

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

## Prebuilt Windows x64 release

The GitHub Releases page provides a ready-to-run, self-contained Windows x64 ZIP. It includes the QuickPlay executable, the required .NET and Windows App SDK runtime files, `bass.dll`, and all applicable licence and third-party notices.

Because the prebuilt package includes BASS under its free non-commercial terms, that package is provided for **non-commercial use only**. Commercial use requires an appropriate BASS licence from Un4seen Developments. This restriction applies to the BASS-powered binary package, not to QuickPlay's own source code.

Extract the ZIP to a normal writable folder and run `QuickPlay.WinUI.exe`. Windows may show a SmartScreen warning because the executable is not code-signed.

## Build and run

Requirements:

- Windows 10 version 1809 or later; Windows 11 recommended.
- Visual Studio with .NET desktop development and WinUI tooling.
- .NET 8 SDK.
- The external BASS DLL installed as described above.

Open `QuickPlay.sln`, select `QuickPlay.WinUI` as the startup project, choose the `x64` platform, and press F5.

To run the behavior checks:

```powershell
dotnet run --project tests\QuickPlay.Tests\QuickPlay.Tests.csproj
```

## Keyboard defaults

- Up/Down: previous/next track.
- Left/Right: seek backward/forward 5 seconds.
- Shift+Left/Right: seek backward/forward 30 seconds.
- Ctrl+Up/Down: previous/next sibling folder.
- Pause: play or pause.
- Ctrl+C: copy the active audio file.
- Delete: confirm and move the active track to the Recycle Bin.

Shortcut assignments can be changed from **Settings → Keyboard**. Audition Start Position and short/long seek durations are available under **Settings → Settings**. If a new shortcut is already in use, QuickPlay asks whether to move it; the previous action then becomes unassigned.

## Known issue in version 1.1

After using the application menu, keyboard focus can remain in a state where arrow keys affect both menu/list navigation and playback. Clicking the track list restores the expected playback shortcut behavior. This is tracked in [TODO.md](TODO.md) for a future focus-routing fix.

## Architecture

- `QuickPlay.Core`: settings, track discovery, metadata, and navigation.
- `QuickPlay.Audio`: BASS-backed playback and playback coordination.
- `QuickPlay.Waveform`: background waveform analysis.
- `QuickPlay.WinUI`: Windows input and presentation.
- `QuickPlay.Tests`: dependency-free behavior checks.

Application settings are stored under `%LOCALAPPDATA%\QuickPlay`. On first launch, QuickPlay copies existing settings from `%LOCALAPPDATA%\DJPlayer` when available.

## Licensing

QuickPlay's own source code uses the [Free-Do-What-You-Want License](LICENSE). Third-party software is excluded from that licence; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
