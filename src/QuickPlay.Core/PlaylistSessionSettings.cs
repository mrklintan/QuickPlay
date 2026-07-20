namespace QuickPlay.Core;

public sealed class PlaylistSessionSettings
{
    public string? FolderPath { get; set; }
    public string? CurrentTrackPath { get; set; }
    public List<string> PlaylistFiles { get; set; } = [];
    public List<string> CompletedFiles { get; set; } = [];

    public bool HasSavedPlaylist =>
        !string.IsNullOrWhiteSpace(FolderPath) && PlaylistFiles.Count > 0;

    public void EnsureValid()
    {
        PlaylistFiles ??= [];
        PlaylistFiles = PlaylistFiles
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        CompletedFiles ??= [];
        CompletedFiles = CompletedFiles
            .Where(path => PlaylistFiles.Contains(path, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(FolderPath) || PlaylistFiles.Count == 0)
            Clear();
    }

    public void Clear()
    {
        FolderPath = null;
        CurrentTrackPath = null;
        PlaylistFiles.Clear();
        CompletedFiles.Clear();
    }
}
