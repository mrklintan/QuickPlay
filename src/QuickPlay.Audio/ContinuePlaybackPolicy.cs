namespace QuickPlay.Audio;

public static class ContinuePlaybackPolicy
{
    public static NaturalPlaybackEndAction Resolve(
        bool continuePlay,
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
