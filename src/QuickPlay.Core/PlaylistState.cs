namespace QuickPlay.Core;

public sealed class PlaylistState
{
    public int Version { get; set; } = 1;
    public string? RootFolder { get; set; }
    public string? CurrentTrack { get; set; }
    public List<PlaylistTrackState> Tracks { get; set; } = [];

    public bool HasSavedPlaylist =>
        !string.IsNullOrWhiteSpace(RootFolder) && Tracks.Count > 0;

    public void EnsureValid()
    {
        Tracks ??= [];
        Tracks = Tracks
            .Where(track => track is not null && !string.IsNullOrWhiteSpace(track.Path))
            .DistinctBy(track => track.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!Tracks.Any(track =>
                string.Equals(track.Path, CurrentTrack, StringComparison.OrdinalIgnoreCase)))
            CurrentTrack = Tracks.FirstOrDefault()?.Path;
        if (string.IsNullOrWhiteSpace(RootFolder) || Tracks.Count == 0)
            Clear();
    }

    public void Clear()
    {
        RootFolder = null;
        CurrentTrack = null;
        Tracks.Clear();
    }
}

public sealed class PlaylistTrackState
{
    public string Path { get; set; } = string.Empty;
    public bool Played { get; set; }
}
