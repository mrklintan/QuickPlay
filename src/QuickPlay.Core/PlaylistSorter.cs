namespace QuickPlay.Core;

public sealed class PlaylistSorter(
    PlaylistColumn column,
    PlaylistSortDirection direction) : IComparer<TrackMetadata>
{
    public int Compare(TrackMetadata? left, TrackMetadata? right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left is null || right is null) return left is null ? 1 : -1;

        var comparison = column switch
        {
            PlaylistColumn.Artist => CompareText(left.Artist, right.Artist),
            PlaylistColumn.Title => CompareText(left.Title, right.Title),
            PlaylistColumn.Album => CompareText(left.Album, right.Album),
            PlaylistColumn.TrackNumber => CompareText(DisplayNumber(left.TrackNumber), DisplayNumber(right.TrackNumber)),
            PlaylistColumn.Year => CompareText(DisplayNumber(left.Year), DisplayNumber(right.Year)),
            PlaylistColumn.Genre => CompareText(left.Genre, right.Genre),
            PlaylistColumn.Comment => CompareText(left.Comment, right.Comment),
            PlaylistColumn.Bpm => CompareText(left.Bpm?.ToString(), right.Bpm?.ToString()),
            PlaylistColumn.Key => CompareText(left.InitialKey, right.InitialKey),
            PlaylistColumn.Energy => CompareText(left.Energy, right.Energy),
            PlaylistColumn.Grouping => CompareText(left.Grouping, right.Grouping),
            PlaylistColumn.Duration => CompareDuration(left.Duration, right.Duration),
            PlaylistColumn.FileName => CompareText(left.FileName, right.FileName),
            PlaylistColumn.FullPath => CompareText(left.FullPath, right.FullPath),
            _ => 0
        };
        if (comparison != 0) return comparison;

        comparison = NaturalStringComparer.Instance.Compare(left.FileName, right.FileName);
        if (comparison != 0) return comparison;
        return NaturalStringComparer.Instance.Compare(left.FullPath, right.FullPath);
    }

    private int CompareText(string? left, string? right) =>
        ApplyDirectionPreservingMissing(left, right, NaturalStringComparer.Instance.Compare(left, right));

    private int CompareDuration(TimeSpan left, TimeSpan right) =>
        ApplyDirectionPreservingMissing(left <= TimeSpan.Zero, right <= TimeSpan.Zero, left.CompareTo(right));

    private int ApplyDirectionPreservingMissing(string? left, string? right, int comparison) =>
        ApplyDirectionPreservingMissing(string.IsNullOrWhiteSpace(left), string.IsNullOrWhiteSpace(right), comparison);

    private int ApplyDirectionPreservingMissing(bool leftMissing, bool rightMissing, int comparison)
    {
        if (leftMissing || rightMissing)
            return leftMissing == rightMissing ? 0 : leftMissing ? 1 : -1;
        return direction == PlaylistSortDirection.Ascending ? comparison : -comparison;
    }

    private static string? DisplayNumber(uint value) => value == 0 ? null : value.ToString();
}
