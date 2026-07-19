namespace QuickPlay.Core;

public sealed class FolderNavigator
{
    public string? MovePrevious(string currentFolderPath) => MoveBy(currentFolderPath, -1);

    public string? MoveNext(string currentFolderPath) => MoveBy(currentFolderPath, 1);

    private static string? MoveBy(string currentFolderPath, int offset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentFolderPath);
        var normalizedCurrent = Path.GetFullPath(Path.TrimEndingDirectorySeparator(currentFolderPath));
        var parent = Directory.GetParent(normalizedCurrent);
        if (parent is null) return null;

        var siblings = Directory.EnumerateDirectories(parent.FullName)
            .Select(path => Path.GetFullPath(Path.TrimEndingDirectorySeparator(path)))
            .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        if (siblings.Length == 0) return null;

        var index = Array.FindIndex(
            siblings,
            path => string.Equals(path, normalizedCurrent, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        return siblings[(index + offset + siblings.Length) % siblings.Length];
    }
}
