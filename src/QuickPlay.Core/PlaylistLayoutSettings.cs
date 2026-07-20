namespace QuickPlay.Core;

public enum PlaylistSortDirection
{
    Ascending,
    Descending
}

public sealed class PlaylistLayoutSettings
{
    private const double MaximumColumnWidth = 1200;

    public List<PlaylistColumn> Columns { get; set; } = [.. PlaylistColumns.DefaultOrder];
    public Dictionary<PlaylistColumn, double> ColumnWidths { get; set; } = CreateDefaultWidths();
    public PlaylistColumn SortColumn { get; set; } = PlaylistColumn.Artist;
    public PlaylistSortDirection SortDirection { get; set; } = PlaylistSortDirection.Ascending;

    public void EnsureValid()
    {
        if (!HasValidColumnOrder(Columns))
            Columns = [.. PlaylistColumns.DefaultOrder];
        else
            Columns = [.. Columns.Distinct()];

        var savedWidths = ColumnWidths ?? [];
        ColumnWidths = PlaylistColumns.All.ToDictionary(
            definition => definition.Column,
            definition => NormalizeWidth(savedWidths, definition));

        if (!Enum.IsDefined(SortColumn) || !Columns.Contains(SortColumn))
            SortColumn = PlaylistColumn.Artist;
        if (!Enum.IsDefined(SortDirection))
            SortDirection = PlaylistSortDirection.Ascending;
    }

    public void ResetColumns()
    {
        Columns = [.. PlaylistColumns.DefaultOrder];
        SortColumn = PlaylistColumn.Artist;
        SortDirection = PlaylistSortDirection.Ascending;
    }

    public static Dictionary<PlaylistColumn, double> CreateDefaultWidths() =>
        PlaylistColumns.All.ToDictionary(definition => definition.Column, definition => definition.DefaultWidth);

    private static bool HasValidColumnOrder(IReadOnlyList<PlaylistColumn>? columns)
    {
        if (columns is null || columns.Count < 2 ||
            columns[0] != PlaylistColumn.Artist || columns[1] != PlaylistColumn.Title)
            return false;

        var seen = new HashSet<PlaylistColumn>();
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            if (!PlaylistColumns.IsSupported(column) || !seen.Add(column)) return false;
            if (index >= 2 && !PlaylistColumns.IsOptional(column)) return false;
        }
        return true;
    }

    private static double NormalizeWidth(
        IReadOnlyDictionary<PlaylistColumn, double> savedWidths,
        PlaylistColumnDefinition definition)
    {
        if (!savedWidths.TryGetValue(definition.Column, out var width) ||
            double.IsNaN(width) || double.IsInfinity(width))
            return definition.DefaultWidth;

        return Math.Clamp(width, definition.MinimumWidth, MaximumColumnWidth);
    }
}
