using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace QuickPlay.WinUI.Services;

public sealed class ClipboardFileService
{
    public async Task CopyFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        package.SetStorageItems([file]);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }
}
