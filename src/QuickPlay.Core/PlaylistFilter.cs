namespace QuickPlay.Core;

public static class PlaylistFilter
{
    public static bool Matches(
        Func<PlaylistColumn, string> getDisplayText,
        IReadOnlyDictionary<PlaylistColumn, string> filters) =>
        filters.All(filter => string.IsNullOrWhiteSpace(filter.Value) ||
            getDisplayText(filter.Key).Contains(filter.Value, StringComparison.OrdinalIgnoreCase));
}
