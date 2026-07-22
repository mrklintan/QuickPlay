using QuickPlay.Audio;
using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class PlaybackControllerTests
{
    public static void Run()
    {
        var backend = new FakeAudioBackend(TimeSpan.FromMinutes(5));
        using var player = new AudioPlayer(backend);
        var queue = new PlaybackQueue();
        queue.SetTracks([new Track("one.mp3"), new Track("two.mp3")]);
        var settings = new ApplicationSettings { AuditionStartPosition = TimeSpan.FromSeconds(75) };
        var controller = new PlaybackController(queue, player, settings);

        var actualPosition = controller.MoveNextAndPlay();

        TestAssert.Equal("two.mp3", backend.OpenedPath);
        TestAssert.Equal(TimeSpan.FromSeconds(75), actualPosition);
        TestAssert.Equal(TimeSpan.FromSeconds(75), backend.SeekPosition);
        TestAssert.True(backend.PlayCalled);

        queue.MarkAllUnplayed();
        queue.SetTracks([new Track("one.mp3"), new Track("two.mp3")]);
        TestAssert.Equal(TimeSpan.Zero, controller.MoveNextAndPlayFrom(TimeSpan.Zero));
        TestAssert.Equal(TimeSpan.Zero, backend.SeekPosition);

        backend.Position = TimeSpan.FromSeconds(75);
        TestAssert.Equal(TimeSpan.FromSeconds(80), controller.SeekBy(TimeSpan.FromSeconds(5)));
        TestAssert.Equal(TimeSpan.FromSeconds(80), backend.SeekPosition);
        TestAssert.Equal(TimeSpan.FromSeconds(150), controller.SeekToFraction(0.5));
        TestAssert.Equal(false, controller.TogglePause());
        TestAssert.Equal(true, controller.TogglePause());

        var current = queue.Current;
        var openCount = backend.OpenCount;
        var playCount = backend.PlayCount;
        queue.ReorderTracks(queue.Tracks);
        TestAssert.True(ReferenceEquals(current, queue.Current));
        TestAssert.Equal(openCount, backend.OpenCount);
        TestAssert.Equal(playCount, backend.PlayCount);
    }

    private sealed class FakeAudioBackend(TimeSpan duration) : IAudioBackend
    {
        public string? OpenedPath { get; private set; }
        public TimeSpan SeekPosition { get; private set; }
        public bool PlayCalled { get; private set; }
        public int OpenCount { get; private set; }
        public int PlayCount { get; private set; }
        public TimeSpan Position { get; set; }
        public bool IsPlaying { get; private set; }
        public TimeSpan Open(string filePath) { OpenedPath = filePath; OpenCount++; return duration; }
        public void Seek(TimeSpan position) => SeekPosition = position;
        public void Play() { PlayCalled = true; PlayCount++; IsPlaying = true; }
        public void Pause() => IsPlaying = false;
        public void Stop() => IsPlaying = false;
        public void Close() => IsPlaying = false;
        public void Dispose() { }
    }
}
