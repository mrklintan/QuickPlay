namespace QuickPlay.Tests;

internal static class Program
{
    public static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            (nameof(AuditionPositionPolicyTests), AuditionPositionPolicyTests.Run),
            (nameof(BassFilePathTests), BassFilePathTests.Run),
            (nameof(PlaylistLayoutSettingsTests), PlaylistLayoutSettingsTests.Run),
            (nameof(NaturalSortingTests), NaturalSortingTests.Run),
            (nameof(PlaybackQueueTests), PlaybackQueueTests.Run),
            (nameof(PlayedTrackPolicyTests), PlayedTrackPolicyTests.Run),
            (nameof(PlaybackControllerTests), PlaybackControllerTests.Run),
            (nameof(SettingsTests), SettingsTests.Run),
            (nameof(TrackCatalogTests), TrackCatalogTests.Run),
            (nameof(TrackNavigatorBoundaryTests), TrackNavigatorBoundaryTests.Run),
            (nameof(FolderNavigatorTests), FolderNavigatorTests.Run),
            (nameof(MetadataTests), MetadataTests.Run),
            (nameof(ShortcutTests), ShortcutTests.Run),
            (nameof(TrackNavigatorRemovalTests), TrackNavigatorRemovalTests.Run)
        };
        try
        {
            foreach (var test in tests)
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            Console.WriteLine($"{tests.Length} test groups passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL {exception.Message}");
            return 1;
        }
    }
}
