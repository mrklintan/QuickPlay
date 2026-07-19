using Microsoft.VisualBasic.FileIO;

namespace QuickPlay.WinUI.Services;

public sealed class ShellFileService
{
    public bool MoveToRecycleBinWithConfirmation(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath)) return false;

        FileSystem.DeleteFile(
            filePath,
            UIOption.AllDialogs,
            RecycleOption.SendToRecycleBin,
            UICancelOption.DoNothing);
        return !File.Exists(filePath);
    }
}
