using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class ExplorerFileSelectorTests
{
    public static void Run()
    {
        TestAssert.Equal(
            "/select,\"C:\\Music Folder\\Track 01.wav\"",
            ExplorerFileSelector.BuildArguments(@"C:\Music Folder\Track 01.wav"));
    }
}
