using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaylistFilterTests
{
    public static void Run()
    {
        var filters = new Dictionary<PlaylistColumn, string> { [PlaylistColumn.Genre] = "dance" };
        foreach (var genre in new[] { "dance", "Dance", "eurodance", "EuroDance hits" })
            TestAssert.Equal(true, PlaylistFilter.Matches(_ => genre, filters));
        TestAssert.Equal(false, PlaylistFilter.Matches(_ => "house", filters));
        TestAssert.Equal(false, PlaylistFilter.Matches(_ => string.Empty, filters));
        filters[PlaylistColumn.Artist] = "abba";
        TestAssert.Equal(true, PlaylistFilter.Matches(c => c == PlaylistColumn.Artist ? "ABBA tribute" : "Eurodance", filters));
        TestAssert.Equal(false, PlaylistFilter.Matches(c => c == PlaylistColumn.Artist ? "Queen" : "Eurodance", filters));
        filters.Clear();
        TestAssert.Equal(true, PlaylistFilter.Matches(_ => string.Empty, filters));
        filters[PlaylistColumn.Title] = " ";
        TestAssert.Equal(true, PlaylistFilter.Matches(_ => string.Empty, filters));
        filters[PlaylistColumn.Title] = ".*";
        TestAssert.Equal(false, PlaylistFilter.Matches(_ => "Any title", filters));
        TestAssert.Equal(true, PlaylistFilter.Matches(_ => "Literal .* title", filters));
    }
}
