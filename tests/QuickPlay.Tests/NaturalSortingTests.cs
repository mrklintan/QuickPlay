using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class NaturalSortingTests
{
    public static void Run()
    {
        var artists = new[] { Metadata(artist: "Zulu"), Metadata(artist: "alpha"), Metadata(artist: "Artist 11"), Metadata(artist: "Artist 2") };
        Array.Sort(artists, new PlaylistSorter(PlaylistColumn.Artist, PlaylistSortDirection.Ascending));
        TestAssert.Equal("alpha,Artist 2,Artist 11,Zulu", string.Join(',', artists.Select(item => item.Artist)));
        Array.Sort(artists, new PlaylistSorter(PlaylistColumn.Artist, PlaylistSortDirection.Descending));
        TestAssert.Equal("Zulu,Artist 11,Artist 2,alpha", string.Join(',', artists.Select(item => item.Artist)));

        var titles = new[] { Metadata(title: "Title 11"), Metadata(title: "title 2"), Metadata(title: "Title 1") };
        Array.Sort(titles, new PlaylistSorter(PlaylistColumn.Title, PlaylistSortDirection.Ascending));
        TestAssert.Equal("Title 1,title 2,Title 11", string.Join(',', titles.Select(item => item.Title)));
        Array.Sort(titles, new PlaylistSorter(PlaylistColumn.Title, PlaylistSortDirection.Descending));
        TestAssert.Equal("Title 11,title 2,Title 1", string.Join(',', titles.Select(item => item.Title)));

        var text = new[] { "Track 11", "track 2", "Track 01", "Track 3" };
        Array.Sort(text, NaturalStringComparer.Instance);
        TestAssert.Equal("Track 01,track 2,Track 3,Track 11", string.Join(',', text));

        var trackNumbers = new[] { Metadata(track: 11), Metadata(track: 2), Metadata(track: 1), Metadata(track: 3) };
        Array.Sort(trackNumbers, new PlaylistSorter(PlaylistColumn.TrackNumber, PlaylistSortDirection.Ascending));
        TestAssert.Equal("1,2,3,11", string.Join(',', trackNumbers.Select(item => item.TrackNumber)));

        var bpm = new[] { Metadata(bpm: 128), Metadata(bpm: null), Metadata(bpm: 9), Metadata(bpm: 100) };
        Array.Sort(bpm, new PlaylistSorter(PlaylistColumn.Bpm, PlaylistSortDirection.Ascending));
        TestAssert.Equal("9,100,128,", string.Join(',', bpm.Select(item => item.Bpm)));

        var energy = new[]
        {
            Metadata(energy: "Unknown"), Metadata(energy: "11"), Metadata(energy: ""),
            Metadata(energy: "2"), Metadata(energy: "01"), Metadata(energy: "7.5")
        };
        Array.Sort(energy, new PlaylistSorter(PlaylistColumn.Energy, PlaylistSortDirection.Ascending));
        TestAssert.Equal("01,2,7.5,11,Unknown,", string.Join(',', energy.Select(item => item.Energy)));

        var durations = new[] { Metadata(seconds: 120), Metadata(seconds: 0), Metadata(seconds: 9), Metadata(seconds: 60) };
        Array.Sort(durations, new PlaylistSorter(PlaylistColumn.Duration, PlaylistSortDirection.Descending));
        TestAssert.Equal("120,60,9,0", string.Join(',', durations.Select(item => (int)item.Duration.TotalSeconds)));
    }

    private static TrackMetadata Metadata(
        uint track = 0,
        string energy = "",
        int seconds = 1,
        uint? bpm = null,
        string artist = "",
        string? title = null) => new(
        Artist: artist,
        Title: title ?? $"Track {track}",
        Album: string.Empty,
        TrackNumber: track,
        Year: 0,
        Genre: string.Empty,
        Comment: string.Empty,
        Bpm: bpm,
        InitialKey: string.Empty,
        Energy: energy,
        Grouping: string.Empty,
        Duration: TimeSpan.FromSeconds(seconds),
        FileName: $"{track}-{energy}.wav",
        FullPath: $@"C:\Music\{track}-{energy}.wav");
}
