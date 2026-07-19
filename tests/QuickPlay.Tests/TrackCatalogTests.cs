using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class TrackCatalogTests
{
    public static void Run()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"QuickPlay-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllBytes(System.IO.Path.Combine(folder, "recording.aif"), []);
            File.WriteAllBytes(System.IO.Path.Combine(folder, "notes.txt"), []);

            var tracks = new TrackCatalog().LoadFolder(folder);

            TestAssert.Equal(1, tracks.Count);
            TestAssert.Equal("recording.aif", tracks[0].DisplayName);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
