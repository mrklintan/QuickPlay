using QuickPlay.Core;

namespace QuickPlay.Audio;

public sealed class PlaybackController(
    PlaybackQueue queue,
    AudioPlayer player,
    ApplicationSettings settings)
{
    public TimeSpan Position => player.Position;
    public TimeSpan Duration => player.Duration;
    public bool IsPlaying => player.IsPlaying;
    public Track? CurrentTrack => player.CurrentTrack;

    public TimeSpan? PlayCurrent() => Play(queue.Current);
    public TimeSpan? MoveNextAndPlay(bool removeCompletedTracks = true) => Play(queue.MoveNext(removeCompletedTracks));
    public TimeSpan? MovePreviousAndPlay(bool removeCompletedTracks = true) => Play(queue.MovePrevious(removeCompletedTracks));
    public TimeSpan? SelectAndPlay(Track track, bool removeCompletedTracks = true) => Play(queue.Select(track, removeCompletedTracks));
    public TimeSpan? SeekBy(TimeSpan offset) => player.SeekBy(offset);
    public bool? TogglePause() => player.TogglePause();
    public TimeSpan? SeekToFraction(double fraction) =>
        player.SeekTo(TimeSpan.FromTicks((long)(player.Duration.Ticks * Math.Clamp(fraction, 0, 1))));

    private TimeSpan? Play(Track? track) => track is null
        ? null
        : player.Play(track, settings.AuditionStartPosition);
}
