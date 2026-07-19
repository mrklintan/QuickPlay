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
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(filePath), SerializerOptions)
                ?? new ApplicationSettings();
            settings.EnsureDefaults();
            return settings;
        }
        catch (JsonException)
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
