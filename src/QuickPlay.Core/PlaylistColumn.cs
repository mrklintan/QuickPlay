namespace QuickPlay.Core;

public enum PlaylistColumn
{
    Artist,
    Title,
    Album,
    TrackNumber,
    Year,
    Genre,
    Comment,
    Bpm,
    Key,
    Energy,
    Grouping,
    Duration,
    FileName,
    FullPath
}

public sealed record PlaylistColumnDefinition(
    PlaylistColumn Column,
    string DisplayName,
    double DefaultWidth,
    double MinimumWidth,
    bool IsFixed);

public static class PlaylistColumns
{
    private static readonly PlaylistColumnDefinition[] Definitions =
    [
        new(PlaylistColumn.Artist, "Artist", 180, 100, true),
        new(PlaylistColumn.Title, "Title", 300, 140, true),
        new(PlaylistColumn.Album, "Album", 220, 120, false),
        new(PlaylistColumn.TrackNumber, "Track Number", 100, 70, false),
        new(PlaylistColumn.Year, "Year", 80, 60, false),
        new(PlaylistColumn.Genre, "Genre", 140, 80, false),
        new(PlaylistColumn.Comment, "Comment", 240, 120, false),
        new(PlaylistColumn.Bpm, "BPM", 80, 55, false),
        new(PlaylistColumn.Key, "Key", 80, 55, false),
        new(PlaylistColumn.Energy, "Energy", 85, 60, false),
        new(PlaylistColumn.Grouping, "Grouping", 160, 90, false),
        new(PlaylistColumn.Duration, "Duration", 90, 70, false),
        new(PlaylistColumn.FileName, "File Name", 260, 140, false),
        new(PlaylistColumn.FullPath, "Full Path", 420, 180, false)
    ];

    public static IReadOnlyList<PlaylistColumnDefinition> All => Definitions;

    public static IReadOnlyList<PlaylistColumn> DefaultOrder { get; } =
    [
        PlaylistColumn.Artist,
        PlaylistColumn.Title,
        PlaylistColumn.Bpm,
        PlaylistColumn.Key,
        PlaylistColumn.Energy,
        PlaylistColumn.Duration
    ];

    public static PlaylistColumnDefinition Get(PlaylistColumn column) =>
        Definitions.First(definition => definition.Column == column);

    public static bool IsSupported(PlaylistColumn column) =>
        Definitions.Any(definition => definition.Column == column);

    public static bool IsOptional(PlaylistColumn column) =>
        IsSupported(column) && !Get(column).IsFixed;
}
