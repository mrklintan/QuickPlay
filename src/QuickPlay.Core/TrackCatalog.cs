namespace QuickPlay.Core;

public sealed class TrackCatalog
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".aac", ".aif", ".aiff", ".flac", ".m4a", ".mp3", ".ogg", ".wav", ".wma" };

    public IReadOnlyList<Track> LoadFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        return EnumerateFilesSafely(Path.GetFullPath(folderPath))
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => Path.GetRelativePath(folderPath, path), NaturalStringComparer.Instance)
            .Select(path => new Track(path))
            .ToArray();
    }

    private static IEnumerable<string> EnumerateFilesSafely(string rootFolder)
    {
        var pending = new Stack<string>();
        pending.Push(rootFolder);
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            string[] files;
            string[] subfolders;
            try
            {
                files = Directory.GetFiles(folder);
                subfolders = Directory.GetDirectories(folder);
            }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }

            foreach (var file in files) yield return file;
            for (var index = subfolders.Length - 1; index >= 0; index--)
                pending.Push(subfolders[index]);
        }
    }
}
