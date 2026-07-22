using QuickPlay.Audio;

namespace QuickPlay.Tests;

internal static class NaturalPlaybackEndPolicyTests
{
    public static void Run()
    {
        var duration = TimeSpan.FromMinutes(4);
        TestAssert.Equal(false, NaturalPlaybackEndPolicy.HasReachedEnd(
            duration - TimeSpan.FromSeconds(1), duration, isPlaying: false, isPausedByUser: false));
        TestAssert.Equal(true, NaturalPlaybackEndPolicy.HasReachedEnd(
            duration - TimeSpan.FromMilliseconds(100), duration, isPlaying: false, isPausedByUser: false));
        TestAssert.Equal(false, NaturalPlaybackEndPolicy.HasReachedEnd(
            duration, duration, isPlaying: true, isPausedByUser: false));
        TestAssert.Equal(false, NaturalPlaybackEndPolicy.HasReachedEnd(
            duration, duration, isPlaying: false, isPausedByUser: true));
        TestAssert.Equal(
            NaturalPlaybackEndAction.Stop,
            NaturalPlaybackEndPolicy.Resolve(
                continuePlay: false, duration, duration, isPlaying: false, isPausedByUser: false));
        TestAssert.Equal(
            NaturalPlaybackEndAction.Continue,
            NaturalPlaybackEndPolicy.Resolve(
                continuePlay: true, duration, duration, isPlaying: false, isPausedByUser: false));
    }
}
