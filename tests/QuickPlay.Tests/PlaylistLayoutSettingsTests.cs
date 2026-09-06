using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaylistLayoutSettingsTests
{
    public static void Run()
    {
        var hiddenFilters = new PlaylistLayoutSettings { ShowFilterButton = false, ShowFilters = true };
        hiddenFilters.EnsureValid();
        TestAssert.Equal(false, hiddenFilters.ShowFilters);
        hiddenFilters.ShowFilterButton = true;
        hiddenFilters.EnsureValid();
        TestAssert.Equal(false, hiddenFilters.ShowFilters);

        var layout = new PlaylistLayoutSettings();
        TestAssert.Equal("Artist,Title,Bpm,Key,Energy,Duration", string.Join(',', layout.Columns));

        layout.Columns = [PlaylistColumn.Title, PlaylistColumn.Artist, PlaylistColumn.Album];
        layout.SortColumn = (PlaylistColumn)999;
        layout.SortDirection = (PlaylistSortDirection)999;
        layout.ColumnWidths[PlaylistColumn.Artist] = 1;
        layout.EnsureValid();

        TestAssert.Equal("Artist,Title,Bpm,Key,Energy,Duration", string.Join(',', layout.Columns));
        TestAssert.Equal(PlaylistColumn.Artist, layout.SortColumn);
        TestAssert.Equal(PlaylistSortDirection.Ascending, layout.SortDirection);
        TestAssert.Equal(PlaylistColumns.Get(PlaylistColumn.Artist).MinimumWidth, layout.ColumnWidths[PlaylistColumn.Artist]);
        TestAssert.True(PlaylistColumns.IsOptional(PlaylistColumn.DiscNumber));
        TestAssert.Equal("Disc Number", PlaylistColumns.Get(PlaylistColumn.DiscNumber).DisplayName);

        layout.Columns = [PlaylistColumn.Artist, PlaylistColumn.Title];
        layout.SortColumn = PlaylistColumn.Energy;
        layout.EnsureValid();
        TestAssert.Equal("Artist,Title", string.Join(',', layout.Columns));
        TestAssert.Equal(PlaylistColumn.Artist, layout.SortColumn);
    }
}
