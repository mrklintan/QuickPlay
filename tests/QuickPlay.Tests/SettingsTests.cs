using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class SettingsTests
{
    public static void Run()
    {
        TestAssert.Equal(TimeSpan.FromMinutes(1), new ApplicationSettings().AuditionStartPosition);
        TestAssert.Equal(true, new ApplicationSettings().ContinuePlay);
        TestAssert.Equal(TimeSpan.FromSeconds(30), new ApplicationSettings().ContinuePlayStartPosition);
        TestAssert.Equal(TimeSpan.FromSeconds(30), new ApplicationSettings().AdvanceBeforeTrackEnd);
        TestAssert.Equal(true, new ApplicationSettings().RemovePlayedTracks);
        TestAssert.Equal(5d, new ApplicationSettings().PlayedThresholdSeconds);
        var folder = Path.Combine(Path.GetTempPath(), $"QuickPlay-{Guid.NewGuid():N}");
        var path = Path.Combine(folder, "settings.json");
        try
        {
            var store = new JsonSettingsStore(path);
            Directory.CreateDirectory(folder);
            File.WriteAllText(
                path,
                """
                {
                  "ContinuePlay": false,
                  "ContinuePlayStartPosition": "00:00:12"
                }
                """);
            var migrated = store.Load();
            TestAssert.Equal(true, migrated.ContinuePlay);
            TestAssert.Equal(TimeSpan.FromSeconds(30), migrated.ContinuePlayStartPosition);
            TestAssert.Equal(TimeSpan.FromSeconds(30), migrated.AdvanceBeforeTrackEnd);

            var settings = new ApplicationSettings
            {
                AuditionStartPosition = TimeSpan.FromSeconds(42),
                ContinuePlay = false,
                ContinuePlayStartPosition = TimeSpan.FromSeconds(12),
                AdvanceBeforeTrackEnd = TimeSpan.FromSeconds(18),
                ShortSeekSeconds = 7,
                LongSeekSeconds = 45,
                RemovePlayedTracks = true,
                PlayedThresholdSeconds = 600
            };
            settings.Shortcuts[ApplicationCommand.PlayPause] = new ShortcutGesture(80);
            settings.Shortcuts[ApplicationCommand.SeekBackwardShort] = ShortcutGesture.Unassigned;
            settings.PlaylistLayout.Columns =
            [
                PlaylistColumn.Artist,
                PlaylistColumn.Title,
                PlaylistColumn.Album,
                PlaylistColumn.Energy
            ];
            settings.PlaylistLayout.ColumnWidths[PlaylistColumn.Artist] = 245;
            settings.PlaylistLayout.SortColumn = PlaylistColumn.Energy;
            settings.PlaylistLayout.SortDirection = PlaylistSortDirection.Descending;
            settings.PlaylistSession.FolderPath = @"C:\Music\Album";
            settings.PlaylistSession.CurrentTrackPath = @"C:\Music\Album\02.wav";
            settings.PlaylistSession.PlaylistFiles =
            [
                @"C:\Music\Album\02.wav",
                @"C:\Music\Album\03.wav"
            ];
            settings.PlaylistSession.CompletedFiles = [@"C:\Music\Album\02.wav"];
            store.Save(settings);
            var loaded = store.Load();
            TestAssert.Equal(TimeSpan.FromSeconds(42), loaded.AuditionStartPosition);
            TestAssert.Equal(false, loaded.ContinuePlay);
            TestAssert.Equal(TimeSpan.FromSeconds(12), loaded.ContinuePlayStartPosition);
            TestAssert.Equal(TimeSpan.FromSeconds(18), loaded.AdvanceBeforeTrackEnd);
            TestAssert.Equal(7d, loaded.ShortSeekSeconds);
            TestAssert.Equal(45d, loaded.LongSeekSeconds);
            TestAssert.Equal(true, loaded.RemovePlayedTracks);
            TestAssert.Equal(600d, loaded.PlayedThresholdSeconds);
            TestAssert.Equal(new ShortcutGesture(80), loaded.Shortcuts[ApplicationCommand.PlayPause]);
            TestAssert.Equal(ShortcutGesture.Unassigned, loaded.Shortcuts[ApplicationCommand.SeekBackwardShort]);
            TestAssert.Equal("Artist,Title,Album,Energy", string.Join(',', loaded.PlaylistLayout.Columns));
            TestAssert.Equal(245d, loaded.PlaylistLayout.ColumnWidths[PlaylistColumn.Artist]);
            TestAssert.Equal(PlaylistColumn.Energy, loaded.PlaylistLayout.SortColumn);
            TestAssert.Equal(PlaylistSortDirection.Descending, loaded.PlaylistLayout.SortDirection);
            TestAssert.Equal(@"C:\Music\Album", loaded.PlaylistSession.FolderPath);
            TestAssert.Equal(@"C:\Music\Album\02.wav", loaded.PlaylistSession.CurrentTrackPath);
            TestAssert.Equal(2, loaded.PlaylistSession.PlaylistFiles.Count);
            TestAssert.Equal(@"C:\Music\Album\02.wav", loaded.PlaylistSession.CompletedFiles.Single());
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
