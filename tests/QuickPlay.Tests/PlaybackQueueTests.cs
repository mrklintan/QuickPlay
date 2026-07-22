using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaybackQueueTests
{
    public static void Run()
    {
        var a = new Track("A.wav");
        var b = new Track("B.wav");
        var c = new Track("C.wav");
        var d = new Track("D.wav");

        var queue = new PlaybackQueue();
        queue.SetTracks([a, b, c, d]);
        TestAssert.Equal("A.wav", queue.Current?.FilePath);
        TestAssert.Equal("A.wav,B.wav,C.wav,D.wav", Paths(queue.VisibleTracks));
        TestAssert.Equal(false, queue.IsCompleted(a));
        queue.MarkCompleted(b);
        TestAssert.True(queue.IsCompleted(b));
        queue.MarkUnplayed(b);
        TestAssert.Equal(false, queue.IsCompleted(b));

        // Leaving an unplayed track keeps it bold and appends it after the remaining order.
        TestAssert.Equal("B.wav", queue.MoveNext(removeCompletedTracks: false)?.FilePath);
        TestAssert.Equal("B.wav,C.wav,D.wav,A.wav", Paths(queue.VisibleTracks));
        TestAssert.Equal("C.wav", queue.MoveNext(removeCompletedTracks: false)?.FilePath);
        TestAssert.Equal("C.wav,D.wav,A.wav,B.wav", Paths(queue.VisibleTracks));

        // A completed track remains normal and is skipped by keyboard navigation.
        queue.MarkCurrentCompleted();
        TestAssert.True(queue.IsCompleted(c));
        TestAssert.Equal("D.wav", queue.MoveNext(removeCompletedTracks: false)?.FilePath);
        TestAssert.Equal("D.wav,A.wav,B.wav,C.wav", Paths(queue.VisibleTracks));
        TestAssert.Equal("C.wav", queue.Select(c, removeCompletedTracks: false)?.FilePath);
        TestAssert.True(queue.IsCompleted(c));
        TestAssert.Equal("A.wav", queue.MoveNext(removeCompletedTracks: false)?.FilePath);

        // Cleanup happens only when leaving a completed current track.
        queue.MarkCurrentCompleted();
        TestAssert.Equal("B.wav", queue.MoveNext(removeCompletedTracks: true)?.FilePath);
        TestAssert.Equal(false, queue.VisibleTracks.Contains(a));

        // Up chooses the last unplayed track in the visible order and skips completed tracks.
        queue.MarkCurrentCompleted();
        TestAssert.Equal("D.wav", queue.MovePrevious(removeCompletedTracks: false)?.FilePath);

        queue.MarkAllUnplayed();
        TestAssert.Equal(0, queue.Completed.Count);
        queue.MarkCurrentCompleted();
        queue.MarkUnplayed(d);
        TestAssert.Equal(false, queue.IsCompleted(d));

        queue.ReorderTracks([d, c, b]);
        TestAssert.Equal("D.wav,C.wav,B.wav", Paths(queue.VisibleTracks));
        TestAssert.Equal("C.wav", queue.MoveNext(removeCompletedTracks: false)?.FilePath);

        var restored = new PlaybackQueue();
        restored.SetTracks([a, b, c], b, [a, b]);
        TestAssert.Equal("B.wav,A.wav,C.wav", Paths(restored.VisibleTracks));
        TestAssert.True(restored.IsCompleted(a));
        TestAssert.True(restored.IsCompleted(b));
        TestAssert.Equal("C.wav", restored.MoveNext(removeCompletedTracks: false)?.FilePath);
        TestAssert.Equal<Track?>(null, restored.MoveNext(removeCompletedTracks: false));
    }

    private static string Paths(IEnumerable<Track> tracks) =>
        string.Join(',', tracks.Select(track => track.FilePath));
}
