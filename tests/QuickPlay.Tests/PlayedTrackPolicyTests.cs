using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlayedTrackPolicyTests
{
    public static void Run()
    {
        var settings = new ApplicationSettings
        {
            RemovePlayedTracks = true,
            PlayedThresholdSeconds = 5
        };

        TestAssert.Equal(false, PlayedTrackPolicy.ShouldRemove(settings, TimeSpan.FromSeconds(4.999)));
        TestAssert.Equal(true, PlayedTrackPolicy.ShouldRemove(settings, TimeSpan.FromSeconds(5)));
        TestAssert.Equal(true, PlayedTrackPolicy.ShouldMarkCompleted(settings, TimeSpan.FromSeconds(5)));

        settings.PlayedThresholdSeconds = 600;
        TestAssert.Equal(false, PlayedTrackPolicy.ShouldRemove(settings, TimeSpan.FromSeconds(599)));
        TestAssert.Equal(true, PlayedTrackPolicy.ShouldRemove(settings, TimeSpan.FromSeconds(600)));

        settings.RemovePlayedTracks = false;
        TestAssert.Equal(false, PlayedTrackPolicy.ShouldRemove(settings, TimeSpan.FromHours(24)));
        TestAssert.Equal(true, PlayedTrackPolicy.ShouldMarkCompleted(settings, TimeSpan.FromSeconds(600)));
    }
}
