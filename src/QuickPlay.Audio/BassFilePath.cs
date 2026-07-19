namespace QuickPlay.Audio;

internal static class BassFilePath
{
    private const int MaxPath = 260;
    private const string ExtendedPathPrefix = @"\\?\";
    private const string ExtendedUncPrefix = @"\\?\UNC\";

    public static string Prepare(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (filePath.Length < MaxPath || filePath.StartsWith(ExtendedPathPrefix, StringComparison.Ordinal))
            return filePath;

        return filePath.StartsWith(@"\\", StringComparison.Ordinal)
            ? ExtendedUncPrefix + filePath[2..]
            : ExtendedPathPrefix + Path.GetFullPath(filePath);
    }
}
