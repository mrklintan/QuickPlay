using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaylistRestoreLogTests
{
    public static void Run()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"QuickPlayLog-{Guid.NewGuid():N}");
        try
        {
            var timestamp = new DateTimeOffset(2026, 7, 22, 14, 30, 15, TimeSpan.FromHours(2));
            var logPath = new PlaylistRestoreLogWriter().Write(
                @"C:\Music\FolderA",
                [new PlaylistRestoreFailure(@"C:\Music\FolderA\CD2\Missing.wav", "File does not exist.")],
                temporaryDirectory,
                timestamp);

            TestAssert.True(File.Exists(logPath));
            TestAssert.True(logPath.EndsWith(@"QuickPlay\PlaylistRestore_20260722_143015_000.txt", StringComparison.Ordinal));
            var text = File.ReadAllText(logPath);
            TestAssert.True(text.Contains("Date/time: 2026-07-22 14:30:15 +02:00", StringComparison.Ordinal));
            TestAssert.True(text.Contains(@"Folder: C:\Music\FolderA", StringComparison.Ordinal));
            TestAssert.True(text.Contains(@"File: C:\Music\FolderA\CD2\Missing.wav", StringComparison.Ordinal));
            TestAssert.True(text.Contains("Reason: File does not exist.", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
        }
    }
}
