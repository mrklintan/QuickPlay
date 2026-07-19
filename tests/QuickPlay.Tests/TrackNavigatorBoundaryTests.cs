using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class TrackNavigatorBoundaryTests
{
    public static void Run()
    {
        var navigator = new TrackNavigator();
        navigator.SetTracks([new Track("one.aif"), new Track("two.aif")]);

        TestAssert.Equal<Track?>(null, navigator.MovePrevious());
        TestAssert.Equal("one.aif", navigator.Current?.FilePath);
        TestAssert.Equal("two.aif", navigator.MoveNext()?.FilePath);
        TestAssert.Equal<Track?>(null, navigator.MoveNext());
        TestAssert.Equal("two.aif", navigator.Current?.FilePath);
    }
}
