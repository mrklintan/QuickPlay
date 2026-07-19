using QuickPlay.Audio;

namespace QuickPlay.Tests;

internal static class BassFilePathTests
{
    public static void Run()
    {
        var shortPath = @"C:\Music\track.wav";
        TestAssert.Equal(shortPath, BassFilePath.Prepare(shortPath));

        var longLocalPath = @"C:\Music\" + new string('a', 250) + ".wav";
        TestAssert.Equal(@"\\?\" + longLocalPath, BassFilePath.Prepare(longLocalPath));

        var longUncPath = @"\\server\share\" + new string('b', 250) + ".wav";
        TestAssert.Equal(
            @"\\?\UNC\server\share\" + new string('b', 250) + ".wav",
            BassFilePath.Prepare(longUncPath));

        var extendedPath = @"\\?\C:\Music\" + new string('c', 250) + ".wav";
        TestAssert.Equal(extendedPath, BassFilePath.Prepare(extendedPath));

        var extendedUncPath = @"\\?\UNC\server\share\" + new string('d', 250) + ".wav";
        TestAssert.Equal(extendedUncPath, BassFilePath.Prepare(extendedUncPath));
    }
}
