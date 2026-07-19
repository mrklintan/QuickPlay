namespace QuickPlay.Audio;

public static class AuditionPositionPolicy
{
    private static readonly TimeSpan MinimumUsefulTail = TimeSpan.FromSeconds(5);

    public static TimeSpan Resolve(TimeSpan configuredPosition, TimeSpan trackDuration)
    {
        if (configuredPosition < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(configuredPosition));
        if (trackDuration <= TimeSpan.Zero) return TimeSpan.Zero;
        if (configuredPosition < trackDuration) return configuredPosition;

        // Preserve at least five seconds of audible material. For very short clips,
        // starting at zero is safer than seeking near or beyond the end.
        return trackDuration <= MinimumUsefulTail
            ? TimeSpan.Zero
            : TimeSpan.FromTicks((trackDuration - MinimumUsefulTail).Ticks / 2);
    }
}
