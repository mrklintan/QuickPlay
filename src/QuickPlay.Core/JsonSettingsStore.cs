using System.Text.Json;

namespace QuickPlay.Core;

public sealed class JsonSettingsStore(string filePath) : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public ApplicationSettings Load()
    {
        if (!File.Exists(filePath)) return new ApplicationSettings();
        try
        {
            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(json, SerializerOptions)
                ?? new ApplicationSettings();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(ApplicationSettings.AdvanceBeforeTrackEnd), out _))
            {
                settings.ContinuePlay = true;
                settings.ContinuePlayStartPosition = ApplicationSettings.DefaultContinuePlayStartPosition;
                settings.AdvanceBeforeTrackEnd = ApplicationSettings.DefaultAdvanceBeforeTrackEnd;
            }
            settings.EnsureDefaults();
            return settings;
        }
        catch (Exception exception) when (exception is JsonException or
                                                   NotSupportedException or
                                                   ArgumentException or
                                                   IOException or
                                                   UnauthorizedAccessException)
        {
            return new ApplicationSettings();
        }
    }

    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        File.WriteAllText(filePath, JsonSerializer.Serialize(settings, SerializerOptions));
    }
}
