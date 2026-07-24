using QuickPlay.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace QuickPlay.WinUI;

public sealed class PlaybackSettingsPage
{
    private readonly TextBox _auditionPositionBox = CreateTimeBox(
        "Audition Start Position",
        "01:00",
        "Manual navigation start. Format: mm:ss");
    private readonly ToggleSwitch _continuePlayToggle = new()
    {
        Header = "Continue Play",
        OnContent = "On",
        OffContent = "Off"
    };
    private readonly ToggleSwitch _djModeToggle = new()
    {
        Header = "DJ Mode",
        OnContent = "On",
        OffContent = "Off",
        Margin = new Thickness(20, 0, 0, 0)
    };
    private readonly TextBox _continuePlayPositionBox = CreateTimeBox(
        "Start next track at",
        "00:30",
        "Automatic start. Format: mm:ss");
    private readonly TextBox _advanceBeforeEndBox = CreateTimeBox(
        "Advance before track end",
        "00:30",
        "00:00 waits for track end. Format: mm:ss");
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
    private readonly ToggleSwitch _removePlayedTracksToggle = new()
    {
        Header = "Remove played tracks from playlist",
        OnContent = "On",
        OffContent = "Off"
    };
    private readonly NumberBox _playedThresholdBox = new()
    {
        Header = "Mark as played after (seconds)",
        Description = "Played tracks use normal text. Tracks left sooner stay bold.",
        Minimum = 0,
        Maximum = 86400,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
    };
    private readonly TextBlock _validationText = new()
    {
        Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
        TextWrapping = TextWrapping.Wrap
    };

    private TimeSpan _validatedAuditionPosition;
    private TimeSpan _validatedContinuePlayPosition;
    private TimeSpan _validatedAdvanceBeforeEnd;

    public PlaybackSettingsPage(ApplicationSettings settings)
    {
        _auditionPositionBox.Text = FormatPosition(settings.AuditionStartPosition);
        _continuePlayToggle.IsOn = settings.ContinuePlay;
        _djModeToggle.IsOn = settings.DjMode;
        _continuePlayPositionBox.Text = FormatPosition(settings.ContinuePlayStartPosition);
        _advanceBeforeEndBox.Text = FormatPosition(settings.AdvanceBeforeTrackEnd);
        _shortSeekBox.Value = settings.ShortSeekSeconds;
        _longSeekBox.Value = settings.LongSeekSeconds;
        _removePlayedTracksToggle.IsOn = settings.RemovePlayedTracks;
        _playedThresholdBox.Value = settings.PlayedThresholdSeconds;
        _continuePlayToggle.Toggled += OnPlaybackModeToggled;
        _djModeToggle.Toggled += OnPlaybackModeToggled;
        UpdatePlaybackModeControls();
        Content = BuildContent();
    }

    public UIElement Content { get; }

    public bool TryValidate()
    {
        _validationText.Text = string.Empty;
        if (!TryParsePosition(_auditionPositionBox.Text, out _validatedAuditionPosition))
            return Fail("Enter Audition Start Position as mm:ss (seconds 00–59).");
        if (!TryParsePosition(_continuePlayPositionBox.Text, out _validatedContinuePlayPosition))
            return Fail("Enter Start next track at as mm:ss (seconds 00–59).");
        if (!TryParsePosition(_advanceBeforeEndBox.Text, out _validatedAdvanceBeforeEnd))
            return Fail("Enter Advance before track end as mm:ss (seconds 00–59).");
        if (double.IsNaN(_shortSeekBox.Value) || double.IsNaN(_longSeekBox.Value) ||
            _shortSeekBox.Value <= 0 || _longSeekBox.Value <= 0)
            return Fail("Seek durations must be positive.");
        if (double.IsNaN(_playedThresholdBox.Value) || _playedThresholdBox.Value < 0)
            return Fail("The played threshold must be zero seconds or more.");
        return true;
    }

    public void ApplyTo(ApplicationSettings settings)
    {
        settings.AuditionStartPosition = _validatedAuditionPosition;
        settings.ContinuePlay = _continuePlayToggle.IsOn;
        settings.DjMode = _djModeToggle.IsOn;
        settings.ContinuePlayStartPosition = _validatedContinuePlayPosition;
        settings.AdvanceBeforeTrackEnd = _validatedAdvanceBeforeEnd;
        settings.ShortSeekSeconds = _shortSeekBox.Value;
        settings.LongSeekSeconds = _longSeekBox.Value;
        settings.RemovePlayedTracks = _removePlayedTracksToggle.IsOn;
        settings.PlayedThresholdSeconds = _playedThresholdBox.Value;
    }

    private UIElement BuildContent()
    {
        var layout = new StackPanel { MinWidth = 580, MaxWidth = 720, Spacing = 18 };
        layout.Children.Add(CreateHeading(
            "Playback",
            "Configure manual auditioning, automatic continuation, seeking, and played-track behavior."));
        layout.Children.Add(_auditionPositionBox);
        layout.Children.Add(_continuePlayToggle);
        layout.Children.Add(CreateDescription(
            "Automatically play the next unplayed track. Full tracks start at 00:00 and play to their natural end."));
        layout.Children.Add(_djModeToggle);
        var djDescription = CreateDescription(
            "Skip configured intros and outros while Continue Play is on.");
        djDescription.Margin = new Thickness(20, 0, 0, 0);
        layout.Children.Add(djDescription);

        var continueGrid = TwoColumnGrid();
        continueGrid.Children.Add(_continuePlayPositionBox);
        Grid.SetColumn(_advanceBeforeEndBox, 1);
        continueGrid.Children.Add(_advanceBeforeEndBox);
        layout.Children.Add(continueGrid);

        layout.Children.Add(CreateSectionHeading("Seeking"));
        var seekGrid = TwoColumnGrid();
        seekGrid.Children.Add(_shortSeekBox);
        Grid.SetColumn(_longSeekBox, 1);
        seekGrid.Children.Add(_longSeekBox);
        layout.Children.Add(seekGrid);

        layout.Children.Add(CreateSectionHeading("Playlist completion"));
        var completionGrid = TwoColumnGrid();
        completionGrid.Children.Add(_playedThresholdBox);
        Grid.SetColumn(_removePlayedTracksToggle, 1);
        completionGrid.Children.Add(_removePlayedTracksToggle);
        layout.Children.Add(completionGrid);
        layout.Children.Add(_validationText);
        return new ScrollViewer
        {
            Content = layout,
            BringIntoViewOnFocusChange = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Enabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Disabled,
            Padding = new Thickness(4, 0, 16, 12)
        };
    }

    private bool Fail(string message)
    {
        _validationText.Text = message;
        return false;
    }

    private void OnPlaybackModeToggled(object sender, RoutedEventArgs e) =>
        UpdatePlaybackModeControls();

    private void UpdatePlaybackModeControls()
    {
        _djModeToggle.IsEnabled = _continuePlayToggle.IsOn;
        var djOptionsVisibility = _continuePlayToggle.IsOn && _djModeToggle.IsOn
            ? Visibility.Visible
            : Visibility.Collapsed;
        _continuePlayPositionBox.Visibility = djOptionsVisibility;
        _advanceBeforeEndBox.Visibility = djOptionsVisibility;
        _continuePlayPositionBox.IsEnabled = _continuePlayToggle.IsOn;
        _advanceBeforeEndBox.IsEnabled = _continuePlayToggle.IsOn;
    }

    private static TextBox CreateTimeBox(string header, string placeholder, string description) => new()
    {
        Header = header,
        PlaceholderText = placeholder,
        Description = description
    };

    private static Grid TwoColumnGrid()
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grid;
    }

    internal static StackPanel CreateHeading(string title, string description) => new()
    {
        Spacing = 4,
        Children =
        {
            new TextBlock { Text = title, FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
            new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                TextWrapping = TextWrapping.Wrap
            }
        }
    };

    private static TextBlock CreateDescription(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
        TextWrapping = TextWrapping.Wrap
    };

    private static TextBlock CreateSectionHeading(string text) => new()
    {
        Text = text,
        Margin = new Thickness(0, 6, 0, -6),
        FontSize = 16,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

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
