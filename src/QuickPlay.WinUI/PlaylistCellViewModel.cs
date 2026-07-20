using System.ComponentModel;
using System.Runtime.CompilerServices;
using QuickPlay.Core;

namespace QuickPlay.WinUI;

public sealed class PlaylistCellViewModel(
    PlaylistColumn column,
    string text,
    double width) : INotifyPropertyChanged
{
    private string _text = text;
    private double _width = width;
    private Windows.UI.Text.FontWeight _fontWeight = Microsoft.UI.Text.FontWeights.Bold;
    private Windows.UI.Text.FontStyle _fontStyle = Windows.UI.Text.FontStyle.Normal;

    public PlaylistColumn Column { get; } = column;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToolTipText));
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            if (Math.Abs(_width - value) < 0.1) return;
            _width = value;
            OnPropertyChanged();
        }
    }

    public Windows.UI.Text.FontWeight FontWeight
    {
        get => _fontWeight;
        set
        {
            if (_fontWeight.Weight == value.Weight) return;
            _fontWeight = value;
            OnPropertyChanged();
        }
    }

    public Windows.UI.Text.FontStyle FontStyle
    {
        get => _fontStyle;
        set
        {
            if (_fontStyle == value) return;
            _fontStyle = value;
            OnPropertyChanged();
        }
    }

    public string? ToolTipText => Column is PlaylistColumn.FullPath or PlaylistColumn.Comment
        ? string.IsNullOrWhiteSpace(Text) ? null : Text
        : null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
