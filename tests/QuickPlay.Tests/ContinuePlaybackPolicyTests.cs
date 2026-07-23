using QuickPlay.Audio;

namespace QuickPlay.Tests;

internal static class ContinuePlaybackPolicyTests
{
    public static void Run()
    {
        var duration = TimeSpan.FromMinutes(4);
        var advance = TimeSpan.FromSeconds(30);

        TestAssert.Equal(
            NaturalPlaybackEndAction.Continue,
            ContinuePlaybackPolicy.Resolve(
                continuePlay: true,
                previousPosition: TimeSpan.FromSeconds(209.8),
                position: TimeSpan.FromSeconds(210.1),
                duration,
                advance,
                isPlaying: true,
                isPausedByUser: false));

        TestAssert.Equal(
            NaturalPlaybackEndAction.None,
            ContinuePlaybackPolicy.Resolve(
                continuePlay: true,
                previousPosition: TimeSpan.FromSeconds(235),
                position: TimeSpan.FromSeconds(235.2),
                duration,
                advance,
                isPlaying: true,
                isPausedByUser: false));

        TestAssert.Equal(
            NaturalPlaybackEndAction.None,
            ContinuePlaybackPolicy.Resolve(
                continuePlay: true,
                previousPosition: TimeSpan.FromSeconds(209.8),
                position: TimeSpan.FromSeconds(210.1),
                duration,
                advance,
                isPlaying: false,
                isPausedByUser: true));

        TestAssert.Equal(
            NaturalPlaybackEndAction.None,
            ContinuePlaybackPolicy.Resolve(
                continuePlay: true,
                previousPosition: TimeSpan.FromSeconds(5),
                position: TimeSpan.FromSeconds(5.2),
                duration: TimeSpan.FromSeconds(20),
                advance,
                isPlaying: true,
                isPausedByUser: false));

        TestAssert.Equal(
            NaturalPlaybackEndAction.Continue,
            ContinuePlaybackPolicy.Resolve(
                continuePlay: true,
                previousPosition: duration - TimeSpan.FromSeconds(1),
                position: duration,
                duration,
                advanceBeforeTrackEnd: TimeSpan.Zero,
                isPlaying: false,
                isPausedByUser: false));

        TestAssert.Equal(
            NaturalPlaybackEndAction.Stop,
            ContinuePlaybackPolicy.Resolve(
                continuePlay: false,
                previousPosition: duration - TimeSpan.FromSeconds(1),
                position: duration,
                duration,
                advance,
                isPlaying: false,
                isPausedByUser: false));
    }
}
