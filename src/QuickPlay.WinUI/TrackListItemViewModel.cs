using System.ComponentModel;
using System.Runtime.CompilerServices;
using QuickPlay.Core;

namespace QuickPlay.WinUI;

public sealed class TrackListItemViewModel : INotifyPropertyChanged
{
    public TrackListItemViewModel() : this(new Track(string.Empty)) { }

    public TrackListItemViewModel(Track track)
    {
        Track = track;
        ApplyMetadata(TrackMetadata.FromFileName(track.FilePath));
    }

    public Track Track { get; set; }
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Bpm { get; set; } = string.Empty;
    public string InitialKey { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public TrackMetadata Metadata { get; set; } = TrackMetadata.FromFileName(string.Empty);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplyMetadata(TrackMetadata metadata)
    {
        Metadata = metadata;
        Artist = metadata.Artist;
        Title = metadata.Title;
        Bpm = metadata.Bpm?.ToString() ?? string.Empty;
        InitialKey = metadata.InitialKey;
        Duration = metadata.Duration > TimeSpan.Zero ? FormatDuration(metadata.Duration) : string.Empty;
        FileName = metadata.FileName;
        OnPropertyChanged(string.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatDuration(TimeSpan duration) =>
        $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
}
