using System.Text.Json;

namespace QuickPlay.Core;

public sealed class JsonSettingsStore(string filePath) : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly object _sync = new();
    private string BackupPath => filePath + ".bak";
    private string TemporaryPath => filePath + ".tmp";

    public ApplicationSettings Load()
    {
        lock (_sync)
        {
            if (TryLoad(filePath, out var settings)) return settings;
            if (TryLoad(BackupPath, out settings)) return settings;
            return new ApplicationSettings();
        }
    }

    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_sync)
        {
            var directory = Path.GetDirectoryName(filePath) ?? ".";
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
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

    private static bool TryLoad(string path, out ApplicationSettings settings)
    {
        settings = new ApplicationSettings();
        if (!File.Exists(path)) return false;
        try
        {
            var json = File.ReadAllText(path);
            settings = JsonSerializer.Deserialize<ApplicationSettings>(json, SerializerOptions)
                ?? new ApplicationSettings();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(ApplicationSettings.AdvanceBeforeTrackEnd), out _))
            {
                settings.ContinuePlay = true;
                settings.ContinuePlayStartPosition = ApplicationSettings.DefaultContinuePlayStartPosition;
                settings.AdvanceBeforeTrackEnd = ApplicationSettings.DefaultAdvanceBeforeTrackEnd;
            }
            settings.EnsureDefaults();
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
