namespace QuickPlay.Core;

public sealed record TrackMetadata(
    string Artist,
    string Title,
    string Album,
    string TrackNumber,
    string DiscNumber,
    uint Year,
    string Genre,
    string Comment,
    uint? Bpm,
    string InitialKey,
    string Energy,
    string Grouping,
    TimeSpan Duration,
    string FileName,
    string FullPath)
{
    public static TrackMetadata FromFileName(string filePath) => new(
        Artist: string.Empty,
        Title: Path.GetFileNameWithoutExtension(filePath),
        Album: string.Empty,
        TrackNumber: string.Empty,
        DiscNumber: string.Empty,
        Year: 0,
        Genre: string.Empty,
        Comment: string.Empty,
        Bpm: null,
        InitialKey: string.Empty,
        Energy: string.Empty,
        Grouping: string.Empty,
        Duration: TimeSpan.Zero,
        FileName: Path.GetFileName(filePath),
        FullPath: filePath);
}
