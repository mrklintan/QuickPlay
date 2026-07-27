using System.Text.Json;

namespace QuickPlay.Core;

public sealed class JsonPlaylistStore(string filePath, string? legacySettingsPath = null) : IPlaylistStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly object _sync = new();
    private string BackupPath => filePath + ".bak";
    private string TemporaryPath => filePath + ".tmp";

    public PlaylistState Load()
    {
        lock (_sync)
        {
            if (TryLoad(filePath, out var playlist)) return playlist;
            if (TryLoad(BackupPath, out playlist)) return playlist;
            if (TryLoadLegacy(out playlist)) return playlist;
            return new PlaylistState();
        }
    }

    public void Save(PlaylistState playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        lock (_sync)
        {
            var directory = Path.GetDirectoryName(filePath) ?? ".";
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(playlist, SerializerOptions);
            try
            {
                File.WriteAllText(TemporaryPath, json);
                if (File.Exists(filePath))
                    File.Replace(TemporaryPath, filePath, BackupPath, ignoreMetadataErrors: true);
                else
                    File.Move(TemporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(TemporaryPath)) File.Delete(TemporaryPath);
            }
        }
    }

    private bool TryLoadLegacy(out PlaylistState playlist)
    {
        playlist = new PlaylistState();
        if (string.IsNullOrWhiteSpace(legacySettingsPath) || !File.Exists(legacySettingsPath))
            return false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(legacySettingsPath));
            if (!document.RootElement.TryGetProperty("PlaylistSession", out var sessionElement))
                return false;
            var session = sessionElement.Deserialize<PlaylistSessionSettings>(SerializerOptions);
            if (session is null) return false;
            session.EnsureValid();
            playlist.RootFolder = session.FolderPath;
            playlist.CurrentTrack = session.CurrentTrackPath;
            playlist.Tracks = session.PlaylistFiles
                .Select(path => new PlaylistTrackState
                {
                    Path = path,
                    Played = session.CompletedFiles.Contains(path, StringComparer.OrdinalIgnoreCase)
                })
                .ToList();
            playlist.EnsureValid();
            return playlist.HasSavedPlaylist;
        }
        catch (Exception exception) when (exception is JsonException or
                                                   NotSupportedException or
                                                   ArgumentException or
                                                   IOException or
                                                   UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryLoad(string path, out PlaylistState playlist)
    {
        playlist = new PlaylistState();
        if (!File.Exists(path)) return false;
        try
        {
            playlist = JsonSerializer.Deserialize<PlaylistState>(
                           File.ReadAllText(path),
                           SerializerOptions)
                       ?? new PlaylistState();
            playlist.EnsureValid();
            return true;
        }
        catch (Exception exception) when (exception is JsonException or
                                                   NotSupportedException or
                                                   ArgumentException or
                                                   IOException or
                                                   UnauthorizedAccessException)
        {
            return false;
        }
    }
}
