namespace QuickPlay.Waveform;

public interface IWaveformAnalyzer
{
    Task<WaveformData> AnalyzeAsync(string filePath, int peakCount, CancellationToken cancellationToken = default);
}
