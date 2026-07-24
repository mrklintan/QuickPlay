namespace QuickPlay.Audio;

public static class ContinuePlaybackPolicy
{
    public static TimeSpan AutomaticStartPosition(bool djMode, TimeSpan djStartPosition) =>
        djMode ? djStartPosition : TimeSpan.Zero;

    public static NaturalPlaybackEndAction Resolve(
        bool continuePlay,
        bool djMode,
        TimeSpan previousPosition,
        TimeSpan position,
        TimeSpan duration,
        TimeSpan advanceBeforeTrackEnd,
        bool isPlaying,
        bool isPausedByUser)
    {
        if (NaturalPlaybackEndPolicy.HasReachedEnd(position, duration, isPlaying, isPausedByUser))
        {
            return continuePlay
                ? NaturalPlaybackEndAction.Continue
                : NaturalPlaybackEndAction.Stop;
        }

        if (!continuePlay ||
            !djMode ||
            !isPlaying ||
            isPausedByUser ||
            advanceBeforeTrackEnd <= TimeSpan.Zero ||
            duration <= advanceBeforeTrackEnd)
        {
            return NaturalPlaybackEndAction.None;
        }

        var threshold = duration - advanceBeforeTrackEnd;
        var crossedThreshold = previousPosition < threshold &&
                               position >= threshold &&
                               position >= previousPosition;
        return crossedThreshold
            ? NaturalPlaybackEndAction.Continue
            : NaturalPlaybackEndAction.None;
    }
}
