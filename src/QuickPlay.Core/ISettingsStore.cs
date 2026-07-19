namespace QuickPlay.Core;

public interface ISettingsStore
{
    ApplicationSettings Load();
    void Save(ApplicationSettings settings);
}
