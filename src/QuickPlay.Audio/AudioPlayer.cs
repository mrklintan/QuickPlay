using QuickPlay.Core;

namespace QuickPlay.Audio;

public sealed class AudioPlayer(IAudioBackend backend) : IDisposable
{
    public Track? CurrentTrack { get; private set; }
    public TimeSpan CurrentStartPosition { get; private set; }
    public TimeSpan Duration { get; private set; }
    public TimeSpan Position => CurrentTrack is null ? TimeSpan.Zero : backend.Position;
    public bool IsPlaying => CurrentTrack is not null && backend.IsPlaying;

    public TimeSpan Play(Track track, TimeSpan auditionStartPosition)
    {
        ArgumentNullException.ThrowIfNull(track);
        backend.Stop();
        Duration = backend.Open(track.FilePath);
        var actualPosition = AuditionPositionPolicy.Resolve(auditionStartPosition, Duration);
        backend.Seek(actualPosition);
        backend.Play();
        CurrentTrack = track;
        CurrentStartPosition = actualPosition;
        return actualPosition;
    }

    public TimeSpan? SeekTo(TimeSpan position)
    {
        if (CurrentTrack is null) return null;
        var actualPosition = position < TimeSpan.Zero
            ? TimeSpan.Zero
            : position > Duration ? Duration : position;
        backend.Seek(actualPosition);
        backend.Play();
        CurrentStartPosition = actualPosition;
        return actualPosition;
    }

    public TimeSpan? SeekBy(TimeSpan offset) => SeekTo(Position + offset);

    public bool? TogglePause()
    {
        if (CurrentTrack is null) return null;
        if (backend.IsPlaying)
        {
            backend.Pause();
            return false;
        }
        backend.Play();
        return true;
    }

    public void Stop() => backend.Stop();

    public void Unload()
    {
        backend.Close();
        CurrentTrack = null;
        CurrentStartPosition = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
    }

    public void Dispose() => backend.Dispose();
}
