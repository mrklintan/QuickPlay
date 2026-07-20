using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaylistLayoutSettingsTests
{
    public static void Run()
    {
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

        layout.Columns = [PlaylistColumn.Artist, PlaylistColumn.Title];
        layout.SortColumn = PlaylistColumn.Energy;
        layout.EnsureValid();
        TestAssert.Equal("Artist,Title", string.Join(',', layout.Columns));
        TestAssert.Equal(PlaylistColumn.Artist, layout.SortColumn);
    }
}
