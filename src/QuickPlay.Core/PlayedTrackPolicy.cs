namespace QuickPlay.Core;

public static class PlayedTrackPolicy
{
    public static bool ShouldMarkCompleted(ApplicationSettings settings, TimeSpan playedTime)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return playedTime >= TimeSpan.FromSeconds(settings.PlayedThresholdSeconds);
    }

    public static bool ShouldRemove(ApplicationSettings settings, TimeSpan playedTime) =>
        settings.RemovePlayedTracks && ShouldMarkCompleted(settings, playedTime);
}
