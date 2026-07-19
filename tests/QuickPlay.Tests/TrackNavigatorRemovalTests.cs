using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class TrackNavigatorRemovalTests
{
    public static void Run()
    {
        var navigator = new TrackNavigator();
        navigator.SetTracks([new Track("one.aif"), new Track("two.aif"), new Track("three.aif")]);
        navigator.Select(1);

        TestAssert.Equal("three.aif", navigator.RemoveCurrent()?.FilePath);
        TestAssert.Equal(2, navigator.Tracks.Count);
        TestAssert.Equal("one.aif", navigator.RemoveCurrent()?.FilePath);
        TestAssert.Equal<Track?>(null, navigator.RemoveCurrent());
    }
}
