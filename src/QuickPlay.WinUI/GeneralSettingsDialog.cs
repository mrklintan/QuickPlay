using QuickPlay.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace QuickPlay.WinUI;

public sealed class GeneralSettingsDialog
{
    private readonly ContentDialog _dialog = new();
    private readonly TextBox _auditionPositionBox = new()
    {
        Header = "Audition Start Position",
        PlaceholderText = "01:00",
        Description = "Time format: mm:ss"
    };
    private readonly NumberBox _shortSeekBox = new()
    {
        Header = "Short Seek Duration (seconds)",
        Minimum = 1,
        Maximum = 300,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
    };
    private readonly NumberBox _longSeekBox = new()
    {
        Header = "Long Seek Duration (seconds)",
        Minimum = 1,
        Maximum = 600,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
    };
    private readonly TextBlock _validationText = new()
    {
        Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
        TextWrapping = TextWrapping.Wrap
    };

    private TimeSpan _validatedAuditionPosition;

    public GeneralSettingsDialog(ApplicationSettings settings)
    {
        _dialog.Title = "Settings";
        _dialog.PrimaryButtonText = "Save";
        _dialog.SecondaryButtonText = "Cancel";
        _dialog.DefaultButton = ContentDialogButton.Primary;
        _dialog.PrimaryButtonClick += OnPrimaryButtonClick;

        _auditionPositionBox.Text = FormatPosition(settings.AuditionStartPosition);
        _shortSeekBox.Value = settings.ShortSeekSeconds;
        _longSeekBox.Value = settings.LongSeekSeconds;
        _dialog.Content = BuildContent();
    }

    public XamlRoot? XamlRoot
    {
        get => _dialog.XamlRoot;
        set => _dialog.XamlRoot = value;
    }

    public async Task<ContentDialogResult> ShowAsync() => await _dialog.ShowAsync();

    public void ApplyTo(ApplicationSettings settings)
    {
        settings.AuditionStartPosition = _validatedAuditionPosition;
        settings.ShortSeekSeconds = _shortSeekBox.Value;
        settings.LongSeekSeconds = _longSeekBox.Value;
    }

    private UIElement BuildContent()
    {
        var layout = new StackPanel { MinWidth = 380, Spacing = 14 };
        layout.Children.Add(_auditionPositionBox);

        var seekGrid = new Grid { ColumnSpacing = 12 };
        seekGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        seekGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        seekGrid.Children.Add(_shortSeekBox);
        Grid.SetColumn(_longSeekBox, 1);
        seekGrid.Children.Add(_longSeekBox);
        layout.Children.Add(seekGrid);
        layout.Children.Add(_validationText);
        return layout;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!TryParsePosition(_auditionPositionBox.Text, out _validatedAuditionPosition))
        {
            _validationText.Text = "Enter the audition position as mm:ss (seconds 00–59).";
            args.Cancel = true;
            return;
        }

        if (double.IsNaN(_shortSeekBox.Value) || double.IsNaN(_longSeekBox.Value) ||
            _shortSeekBox.Value <= 0 || _longSeekBox.Value <= 0)
        {
            _validationText.Text = "Seek durations must be positive.";
            args.Cancel = true;
        }
    }

    private static string FormatPosition(TimeSpan position) =>
        $"{(int)position.TotalMinutes:00}:{position.Seconds:00}";

    private static bool TryParsePosition(string text, out TimeSpan position)
    {
        position = TimeSpan.Zero;
        var parts = text.Trim().Split(':');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var minutes) ||
            !int.TryParse(parts[1], out var seconds) ||
            minutes < 0 || seconds is < 0 or > 59)
            return false;

        position = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        return true;
    }
}
