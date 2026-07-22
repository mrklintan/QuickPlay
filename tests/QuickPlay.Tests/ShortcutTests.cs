using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class ShortcutTests
{
    public static void Run()
    {
        var settings = new ApplicationSettings();
        var manager = new ShortcutManager(settings);

        TestAssert.Equal(
            ApplicationCommand.NextTrack,
            manager.Resolve(new ShortcutGesture(40)));
        TestAssert.Equal(
            ApplicationCommand.PlayPause,
            manager.Resolve(new ShortcutGesture(32)));
        TestAssert.Equal(
            ApplicationCommand.SeekBackwardShort,
            manager.Resolve(new ShortcutGesture(37, ShortcutModifiers.Control)));
        TestAssert.Equal(
            ApplicationCommand.SeekBackwardLong,
            manager.Resolve(new ShortcutGesture(37)));
        TestAssert.Equal(
            ApplicationCommand.CopyCurrentTrack,
            manager.Resolve(new ShortcutGesture(67, ShortcutModifiers.Control)));
        TestAssert.Equal(
            ApplicationCommand.DeleteCurrentTrack,
            manager.Resolve(new ShortcutGesture(46)));
        TestAssert.Equal(
            ShortcutDefaults.Create().Count,
            ShortcutDefaults.Create().Values.Distinct().Count());

        settings.Shortcuts[ApplicationCommand.NextTrack] = new ShortcutGesture(78);
        TestAssert.Equal(ApplicationCommand.NextTrack, manager.Resolve(new ShortcutGesture(78)));
        TestAssert.Equal<ApplicationCommand?>(null, manager.Resolve(new ShortcutGesture(40)));

        settings.Shortcuts[ApplicationCommand.NextTrack] = ShortcutGesture.Unassigned;
        TestAssert.Equal("Unassigned", settings.Shortcuts[ApplicationCommand.NextTrack].DisplayText);
        TestAssert.Equal<ApplicationCommand?>(null, manager.Resolve(ShortcutGesture.Unassigned));
    }
}
