using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class SettingsTests
{
    public static void Run()
    {
        TestAssert.Equal(TimeSpan.FromMinutes(1), new ApplicationSettings().AuditionStartPosition);
        var folder = Path.Combine(Path.GetTempPath(), $"QuickPlay-{Guid.NewGuid():N}");
        var path = Path.Combine(folder, "settings.json");
        try
        {
            var store = new JsonSettingsStore(path);
            var settings = new ApplicationSettings
            {
                AuditionStartPosition = TimeSpan.FromSeconds(42),
                ShortSeekSeconds = 7,
                LongSeekSeconds = 45
            };
            settings.Shortcuts[ApplicationCommand.PlayPause] = new ShortcutGesture(32);
            settings.Shortcuts[ApplicationCommand.SeekBackwardShort] = ShortcutGesture.Unassigned;
            store.Save(settings);
            var loaded = store.Load();
            TestAssert.Equal(TimeSpan.FromSeconds(42), loaded.AuditionStartPosition);
            TestAssert.Equal(7d, loaded.ShortSeekSeconds);
            TestAssert.Equal(45d, loaded.LongSeekSeconds);
            TestAssert.Equal(new ShortcutGesture(32), loaded.Shortcuts[ApplicationCommand.PlayPause]);
            TestAssert.Equal(ShortcutGesture.Unassigned, loaded.Shortcuts[ApplicationCommand.SeekBackwardShort]);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
