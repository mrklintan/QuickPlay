using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace QuickPlay.WinUI;

public partial class App : Application
{
    private const string BassDownloadUrl = "https://www.un4seen.com/bass.html";
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var bassPath = Path.Combine(AppContext.BaseDirectory, "bass.dll");
        if (!File.Exists(bassPath))
        {
            ShowBassProblem("bass.dll is missing.");
            return;
        }

        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (DllNotFoundException)
        {
            ShowBassProblem("bass.dll could not be loaded.");
        }
        catch (BadImageFormatException)
        {
            ShowBassProblem("bass.dll is not the 64-bit version required by QuickPlay.");
        }
    }

    private static void ShowBassProblem(string reason)
    {
        var result = MessageBox(
            nint.Zero,
            $"{reason}\n\nDownload the 64-bit BASS library from:\n{BassDownloadUrl}\n\nPlace bass.dll in the same folder as QuickPlay.WinUI.exe.\n\nOpen the download page now?",
            "QuickPlay - BASS is required",
            0x00000004 | 0x00000010);
        if (result == 6)
            ShellExecute(nint.Zero, "open", BassDownloadUrl, null, null, 1);
    }

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(nint window, string text, string caption, uint type);

    [LibraryImport("shell32.dll", EntryPoint = "ShellExecuteW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint ShellExecute(
        nint window,
        string operation,
        string file,
        string? parameters,
        string? directory,
        int showCommand);
}
