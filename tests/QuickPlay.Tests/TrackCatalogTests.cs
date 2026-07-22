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
            var cd1 = Directory.CreateDirectory(System.IO.Path.Combine(folder, "CD1")).FullName;
            var cd2 = Directory.CreateDirectory(System.IO.Path.Combine(folder, "CD2")).FullName;
            File.WriteAllBytes(System.IO.Path.Combine(cd1, "Track02.wav"), []);
            File.WriteAllBytes(System.IO.Path.Combine(cd1, "Track01.mp3"), []);
            File.WriteAllBytes(System.IO.Path.Combine(cd2, "Track01.flac"), []);

            var tracks = new TrackCatalog().LoadFolder(folder);

            TestAssert.Equal(4, tracks.Count);
            TestAssert.Equal("Track01.mp3", tracks[0].DisplayName);
            TestAssert.Equal("Track02.wav", tracks[1].DisplayName);
            TestAssert.Equal("Track01.flac", tracks[2].DisplayName);
            TestAssert.Equal("recording.aif", tracks[3].DisplayName);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
