namespace QuickPlay.Core;

public sealed class Track
{
    public Track(string filePath) => FilePath = filePath;

    public string FilePath { get; set; }

    public string DisplayName => Path.GetFileName(FilePath);
}
