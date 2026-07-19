namespace QuickPlay.Core;

public interface ITrackMetadataReader
{
    Task<TrackMetadata> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
