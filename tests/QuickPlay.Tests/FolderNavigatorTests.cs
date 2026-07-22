using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class FolderNavigatorTests
{
    public static void Run()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"QuickPlay-{Guid.NewGuid():N}");
        var first = Directory.CreateDirectory(System.IO.Path.Combine(root, "Album 1-4")).FullName;
        var second = Directory.CreateDirectory(System.IO.Path.Combine(root, "Album 5-8")).FullName;
        var third = Directory.CreateDirectory(System.IO.Path.Combine(root, "Album 9-12")).FullName;
        Directory.CreateDirectory(System.IO.Path.Combine(first, "CD1"));
        Directory.CreateDirectory(System.IO.Path.Combine(first, "CD2"));
        try
        {
            var navigator = new FolderNavigator();

            TestAssert.Equal(second, navigator.MoveNext(first));
            TestAssert.True(!string.Equals(
                System.IO.Path.Combine(first, "CD2"), navigator.MoveNext(first), StringComparison.OrdinalIgnoreCase));
            TestAssert.Equal(third, navigator.MoveNext(second));
            TestAssert.Equal(first, navigator.MovePrevious(second));
            TestAssert.Equal<string?>(null, navigator.MovePrevious(first));
            TestAssert.Equal<string?>(null, navigator.MoveNext(third));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
