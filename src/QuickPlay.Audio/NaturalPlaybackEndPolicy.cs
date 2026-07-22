namespace QuickPlay.Audio;

public enum NaturalPlaybackEndAction
{
    None,
    Stop,
    Continue
}

public static class NaturalPlaybackEndPolicy
{
    private static readonly TimeSpan EndTolerance = TimeSpan.FromMilliseconds(350);

    public static bool HasReachedEnd(
        TimeSpan position,
        TimeSpan duration,
        bool isPlaying,
        bool isPausedByUser)
    {
        if (isPlaying || isPausedByUser || duration <= TimeSpan.Zero) return false;
        return duration - position <= EndTolerance;
    }

    public static NaturalPlaybackEndAction Resolve(
        bool continuePlay,
        TimeSpan position,
        TimeSpan duration,
        bool isPlaying,
        bool isPausedByUser)
    {
        if (!HasReachedEnd(position, duration, isPlaying, isPausedByUser))
            return NaturalPlaybackEndAction.None;
        return continuePlay
            ? NaturalPlaybackEndAction.Continue
            : NaturalPlaybackEndAction.Stop;
    }
}
