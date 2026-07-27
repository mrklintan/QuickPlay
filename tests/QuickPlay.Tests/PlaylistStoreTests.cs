using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaylistStoreTests
{
    public static void Run()
    {
        SaveLoadBackupAndMigration();
        CoalescesRapidChanges();
        SkipsUnchangedState();
    }

    private static void SaveLoadBackupAndMigration()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"QuickPlay-Playlist-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "playlist.json");
        var legacyPath = Path.Combine(folder, "settings.json");
        try
        {
            File.WriteAllText(
                legacyPath,
                """
                {
                  "PlaylistSession": {
                    "FolderPath": "C:\\Music\\Legacy",
                    "CurrentTrackPath": "C:\\Music\\Legacy\\02.wav",
                    "PlaylistFiles": [
                      "C:\\Music\\Legacy\\02.wav",
                      "C:\\Music\\Legacy\\03.wav"
                    ],
                    "CompletedFiles": ["C:\\Music\\Legacy\\02.wav"]
                  }
                }
                """);
            var store = new JsonPlaylistStore(path, legacyPath);
            var migrated = store.Load();
            TestAssert.Equal(@"C:\Music\Legacy", migrated.RootFolder);
            TestAssert.Equal(2, migrated.Tracks.Count);
            TestAssert.Equal(true, migrated.Tracks[0].Played);
            TestAssert.Equal(false, migrated.Tracks[1].Played);

            store.Save(migrated);
            migrated.CurrentTrack = migrated.Tracks[1].Path;
            migrated.Tracks[1].Played = true;
            store.Save(migrated);
            TestAssert.Equal(true, File.Exists(path + ".bak"));

            File.WriteAllText(path, """{ "Tracks": [""");
            var recovered = store.Load();
            TestAssert.Equal(@"C:\Music\Legacy\02.wav", recovered.CurrentTrack);
            TestAssert.Equal(false, recovered.Tracks[1].Played);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static void CoalescesRapidChanges()
    {
        var store = new BlockingPlaylistStore();
        var coordinator = new PlaylistSaveCoordinator(store);
        var first = coordinator.QueueSave(State("first.wav"));
        store.Started.Wait();
        var second = coordinator.QueueSave(State("second.wav"));
        var third = coordinator.QueueSave(State("third.wav"));
        store.Release.Set();
        Task.WhenAll(first, second, third).GetAwaiter().GetResult();

        TestAssert.Equal(2, store.Saved.Count);
        TestAssert.Equal("third.wav", store.Saved[^1].CurrentTrack);
    }

    private static void SkipsUnchangedState()
    {
        var persisted = State("first.wav");
        var store = new RecordingPlaylistStore();
        var coordinator = new PlaylistSaveCoordinator(store, persisted);

        coordinator.QueueSave(State("first.wav")).GetAwaiter().GetResult();
        TestAssert.Equal(0, store.Saved.Count);

        coordinator.QueueSave(State("second.wav")).GetAwaiter().GetResult();
        coordinator.QueueSave(State("second.wav")).GetAwaiter().GetResult();
        TestAssert.Equal(1, store.Saved.Count);
        TestAssert.Equal("second.wav", store.Saved[0].CurrentTrack);
    }

    private static PlaylistState State(string path) => new()
    {
        RootFolder = @"C:\Music",
        CurrentTrack = path,
        Tracks = [new PlaylistTrackState { Path = path }]
    };

    private sealed class BlockingPlaylistStore : IPlaylistStore
    {
        public ManualResetEventSlim Started { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public List<PlaylistState> Saved { get; } = [];

        public PlaylistState Load() => new();

        public void Save(PlaylistState playlist)
        {
            Started.Set();
            Release.Wait();
            lock (Saved) Saved.Add(playlist);
        }
    }

    private sealed class RecordingPlaylistStore : IPlaylistStore
    {
        public List<PlaylistState> Saved { get; } = [];

        public PlaylistState Load() => new();

        public void Save(PlaylistState playlist) => Saved.Add(playlist);
    }
}
