using QuickPlay.Audio;

namespace QuickPlay.Tests;

internal static class AuditionPositionPolicyTests
{
    public static void Run()
    {
        TestAssert.Equal(TimeSpan.FromMinutes(1), AuditionPositionPolicy.Resolve(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(4)));
        TestAssert.Equal(TimeSpan.Zero, AuditionPositionPolicy.Resolve(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(4)));
        TestAssert.Equal(TimeSpan.FromSeconds(12.5), AuditionPositionPolicy.Resolve(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30)));
    }
}
