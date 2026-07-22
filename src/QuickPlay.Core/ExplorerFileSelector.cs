using System.Diagnostics;

namespace QuickPlay.Core;

public sealed class ExplorerFileSelector
{
    public static string BuildArguments(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return $"/select,\"{filePath}\"";
    }

    public void Select(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = BuildArguments(filePath),
            UseShellExecute = true
        });
    }
}
