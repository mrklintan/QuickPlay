using System.Collections.ObjectModel;
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

    public Track Track { get; }
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Bpm { get; set; } = string.Empty;
    public string InitialKey { get; set; } = string.Empty;
    public string Energy { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public TrackMetadata Metadata { get; set; } = TrackMetadata.FromFileName(string.Empty);
    public ObservableCollection<PlaylistCellViewModel> Cells { get; } = [];
    public bool IsCompleted { get; private set; }
    public bool IsCurrent { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplyMetadata(TrackMetadata metadata)
    {
        Metadata = metadata;
        Artist = metadata.Artist;
        Title = metadata.Title;
        Bpm = metadata.Bpm?.ToString() ?? string.Empty;
        InitialKey = metadata.InitialKey;
        Energy = metadata.Energy;
        Duration = metadata.Duration > TimeSpan.Zero ? FormatDuration(metadata.Duration) : string.Empty;
        FileName = metadata.FileName;
        foreach (var cell in Cells) cell.Text = GetDisplayText(cell.Column);
        OnPropertyChanged(string.Empty);
    }

    public void ConfigureColumns(
        IReadOnlyList<PlaylistColumn> columns,
        IReadOnlyDictionary<PlaylistColumn, double> widths)
    {
        Cells.Clear();
        foreach (var column in columns)
        {
            var definition = PlaylistColumns.Get(column);
            var width = widths.TryGetValue(column, out var savedWidth)
                ? Math.Max(definition.MinimumWidth, savedWidth)
                : definition.DefaultWidth;
            var cell = new PlaylistCellViewModel(column, GetDisplayText(column), width);
            ApplyPlaybackStyle(cell);
            Cells.Add(cell);
        }
    }

    public void SetPlaybackState(bool isCompleted, bool isCurrent)
    {
        IsCompleted = isCompleted;
        IsCurrent = isCurrent;
        foreach (var cell in Cells) ApplyPlaybackStyle(cell);
    }

    public void SetColumnWidth(PlaylistColumn column, double width)
    {
        var cell = Cells.FirstOrDefault(candidate => candidate.Column == column);
        if (cell is not null) cell.Width = width;
    }

    public string GetDisplayText(PlaylistColumn column) => column switch
    {
        PlaylistColumn.Artist => Metadata.Artist ?? string.Empty,
        PlaylistColumn.Title => Metadata.Title ?? string.Empty,
        PlaylistColumn.Album => Metadata.Album ?? string.Empty,
        PlaylistColumn.TrackNumber => Metadata.TrackNumber == 0 ? string.Empty : Metadata.TrackNumber.ToString(),
        PlaylistColumn.Year => Metadata.Year == 0 ? string.Empty : Metadata.Year.ToString(),
        PlaylistColumn.Genre => Metadata.Genre ?? string.Empty,
        PlaylistColumn.Comment => Metadata.Comment ?? string.Empty,
        PlaylistColumn.Bpm => Metadata.Bpm?.ToString() ?? string.Empty,
        PlaylistColumn.Key => Metadata.InitialKey ?? string.Empty,
        PlaylistColumn.Energy => Metadata.Energy ?? string.Empty,
        PlaylistColumn.Grouping => Metadata.Grouping ?? string.Empty,
        PlaylistColumn.Duration => Metadata.Duration > TimeSpan.Zero ? FormatDuration(Metadata.Duration) : string.Empty,
        PlaylistColumn.FileName => Metadata.FileName ?? string.Empty,
        PlaylistColumn.FullPath => Metadata.FullPath ?? string.Empty,
        _ => string.Empty
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void ApplyPlaybackStyle(PlaylistCellViewModel cell)
    {
        cell.FontWeight = IsCompleted
            ? Microsoft.UI.Text.FontWeights.Normal
            : Microsoft.UI.Text.FontWeights.Bold;
        cell.FontStyle = IsCurrent
            ? Windows.UI.Text.FontStyle.Italic
            : Windows.UI.Text.FontStyle.Normal;
    }

    private static string FormatDuration(TimeSpan duration) =>
        $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
}
