using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class MetadataTests
{
    public static void Run()
    {
        var fallback = TrackMetadata.FromFileName(@"C:\Music\Artist - Title.aif");
        TestAssert.Equal("Artist - Title", fallback.Title);
        TestAssert.Equal("Artist - Title.aif", fallback.FileName);

        var missingPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"missing-{Guid.NewGuid():N}.aif");
        var loaded = new TagLibTrackMetadataReader().ReadAsync(missingPath).GetAwaiter().GetResult();
        TestAssert.Equal(System.IO.Path.GetFileNameWithoutExtension(missingPath), loaded.Title);
    }
}
