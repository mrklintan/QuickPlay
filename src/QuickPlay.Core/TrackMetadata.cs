namespace QuickPlay.Core;

public sealed record TrackMetadata(
    string Artist,
    string Title,
    string Album,
    uint TrackNumber,
    uint Year,
    string Genre,
    string Comment,
    uint? Bpm,
    string Grouping,
    string InitialKey,
    TimeSpan Duration,
    string FileName,
    string FullPath)
{
    public static TrackMetadata FromFileName(string filePath) => new(
        Artist: string.Empty,
        Title: Path.GetFileNameWithoutExtension(filePath),
        Album: string.Empty,
        TrackNumber: 0,
        Year: 0,
        Genre: string.Empty,
        Comment: string.Empty,
        Bpm: null,
        Grouping: string.Empty,
        InitialKey: string.Empty,
        Duration: TimeSpan.Zero,
        FileName: Path.GetFileName(filePath),
        FullPath: filePath);
}
