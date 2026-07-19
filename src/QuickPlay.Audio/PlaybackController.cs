using QuickPlay.Core;

namespace QuickPlay.Audio;

public sealed class PlaybackController(
    TrackNavigator navigator,
    AudioPlayer player,
    ApplicationSettings settings)
{
    public TimeSpan Position => player.Position;
    public TimeSpan Duration => player.Duration;
    public bool IsPlaying => player.IsPlaying;
    public Track? CurrentTrack => player.CurrentTrack;

    public TimeSpan? PlayCurrent() => Play(navigator.Current);
    public TimeSpan? MoveNextAndPlay() => Play(navigator.MoveNext());
    public TimeSpan? MovePreviousAndPlay() => Play(navigator.MovePrevious());
    public TimeSpan? SelectAndPlay(int index) => Play(navigator.Select(index));
    public TimeSpan? SeekBy(TimeSpan offset) => player.SeekBy(offset);
    public bool? TogglePause() => player.TogglePause();
    public TimeSpan? SeekToFraction(double fraction) =>
        player.SeekTo(TimeSpan.FromTicks((long)(player.Duration.Ticks * Math.Clamp(fraction, 0, 1))));

    private TimeSpan? Play(Track? track) => track is null
        ? null
        : player.Play(track, settings.AuditionStartPosition);
}
