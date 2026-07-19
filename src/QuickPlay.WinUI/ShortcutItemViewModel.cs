using System.ComponentModel;
using System.Runtime.CompilerServices;
using QuickPlay.Core;

namespace QuickPlay.WinUI;

public sealed class ShortcutItemViewModel : INotifyPropertyChanged
{
    private ShortcutGesture _gesture = new(0);

    public ApplicationCommand Command { get; set; }
    public string Label { get; set; } = string.Empty;

    public ShortcutGesture Gesture
    {
        get => _gesture;
        set
        {
            _gesture = value;
            Display = value.DisplayText;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Display));
        }
    }

    public string Display { get; set; } = string.Empty;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
