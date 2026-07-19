namespace QuickPlay.Core;

public sealed class TagLibTrackMetadataReader : ITrackMetadataReader
{
    public Task<TrackMetadata> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Task.Run(() => Read(filePath, cancellationToken), cancellationToken);
    }

    private static TrackMetadata Read(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fallback = TrackMetadata.FromFileName(filePath);
        try
        {
            using var mediaFile = TagLib.File.Create(filePath);
            cancellationToken.ThrowIfCancellationRequested();
            var tag = mediaFile.Tag;
            var artist = FirstNonEmpty(tag.JoinedPerformers, tag.FirstPerformer);
            var title = FirstNonEmpty(tag.Title, fallback.Title);
            var grouping = tag.Grouping ?? string.Empty;
            return new TrackMetadata(
                Artist: artist,
                Title: title,
                Album: tag.Album ?? string.Empty,
                TrackNumber: tag.Track,
                Year: tag.Year,
                Genre: tag.JoinedGenres ?? string.Empty,
                Comment: tag.Comment ?? string.Empty,
                Bpm: tag.BeatsPerMinute == 0 ? null : tag.BeatsPerMinute,
                Grouping: grouping,
                InitialKey: ReadInitialKey(mediaFile),
                Duration: mediaFile.Properties.Duration,
                FileName: fallback.FileName,
                FullPath: filePath);
        }
        catch (Exception exception) when (exception is TagLib.CorruptFileException or
                                                   TagLib.UnsupportedFormatException or
                                                   IOException or
                                                   UnauthorizedAccessException)
        {
            return fallback;
        }
    }

    private static string ReadInitialKey(TagLib.File mediaFile)
    {
        if (mediaFile.GetTag(TagLib.TagTypes.Id3v2, false) is TagLib.Id3v2.Tag id3)
        {
            var keyFrame = id3.GetFrames<TagLib.Id3v2.TextInformationFrame>()
                .FirstOrDefault(frame => frame.FrameId.ToString() == "TKEY");
            var key = keyFrame?.Text?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(key)) return key.Trim();

            var userKey = id3.GetFrames<TagLib.Id3v2.UserTextInformationFrame>()
                .FirstOrDefault(frame =>
                    frame.Description.Equals("INITIALKEY", StringComparison.OrdinalIgnoreCase) ||
                    frame.Description.Equals("INITIAL KEY", StringComparison.OrdinalIgnoreCase) ||
                    frame.Description.Equals("KEY", StringComparison.OrdinalIgnoreCase));
            key = userKey?.Text?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(key)) return key.Trim();
        }

        if (mediaFile.GetTag(TagLib.TagTypes.Xiph, false) is TagLib.Ogg.XiphComment xiph)
        {
            foreach (var fieldName in new[] { "INITIALKEY", "INITIAL KEY", "KEY" })
            {
                var key = xiph.GetField(fieldName).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(key)) return key.Trim();
            }
        }
        return string.Empty;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
