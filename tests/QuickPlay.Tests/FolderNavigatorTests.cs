using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class FolderNavigatorTests
{
    public static void Run()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"QuickPlay-{Guid.NewGuid():N}");
        var first = Directory.CreateDirectory(System.IO.Path.Combine(root, "Album 1-4")).FullName;
        var second = Directory.CreateDirectory(System.IO.Path.Combine(root, "Album 5-8")).FullName;
        try
        {
            var navigator = new FolderNavigator();

            TestAssert.Equal(second, navigator.MoveNext(first));
            TestAssert.Equal(first, navigator.MovePrevious(second));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
