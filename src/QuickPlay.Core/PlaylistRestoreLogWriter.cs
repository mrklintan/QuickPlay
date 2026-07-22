using System.Globalization;
using System.Text;

namespace QuickPlay.Core;

public sealed record PlaylistRestoreFailure(string FilePath, string Reason);

public sealed class PlaylistRestoreLogWriter
{
    public string Write(string restoredFolder, IReadOnlyCollection<PlaylistRestoreFailure> failures,
        string? temporaryDirectory = null, DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(restoredFolder);
        ArgumentNullException.ThrowIfNull(failures);
        if (failures.Count == 0) throw new ArgumentException("At least one failure is required.", nameof(failures));

        var date = timestamp ?? DateTimeOffset.Now;
        var directory = Path.Combine(temporaryDirectory ?? Path.GetTempPath(), "QuickPlay");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"PlaylistRestore_{date:yyyyMMdd_HHmmss_fff}.txt");
        var text = new StringBuilder()
            .AppendLine("QuickPlay playlist restore report")
            .AppendLine($"Date/time: {date.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}")
            .AppendLine($"Folder: {restoredFolder}")
            .AppendLine($"Files not restored: {failures.Count}")
            .AppendLine();
        foreach (var failure in failures)
        {
            text.AppendLine($"File: {failure.FilePath}");
            text.AppendLine($"Reason: {failure.Reason}");
            text.AppendLine();
        }
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        return path;
    }
}
