namespace QuickPlay.Audio;

public interface IAudioBackend : IDisposable
{
    TimeSpan Position { get; }
    bool IsPlaying { get; }
    TimeSpan Open(string filePath);
    void Seek(TimeSpan position);
    void Play();
    void Pause();
    void Stop();
    void Close();
}
