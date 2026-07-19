namespace QuickPlay.Tests;

internal static class TestAssert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }

    public static void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected true, received false.");
    }
}
