using QuickPlay.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;

namespace QuickPlay.WinUI;

public sealed class KeyboardSettingsPage
{
    private readonly nint _ownerWindowHandle;
    private readonly Dictionary<ApplicationCommand, ShortcutGesture> _shortcuts = [];
    private readonly Dictionary<ApplicationCommand, TextBlock> _gestureLabels = [];
    private readonly TextBlock _captureMessage = new()
    {
        Text = "Select an action, then press its new shortcut.",
        TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBlock _warningText = new()
    {
        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center
    };

    private ApplicationCommand? _capturingCommand;

    public KeyboardSettingsPage(ApplicationSettings settings, nint ownerWindowHandle)
    {
        _ownerWindowHandle = ownerWindowHandle;
        var defaults = ShortcutDefaults.Create();
        foreach (var command in Enum.GetValues<ApplicationCommand>())
        {
            _shortcuts[command] = settings.Shortcuts.TryGetValue(command, out var gesture)
                ? gesture
                : defaults[command];
        }

        Content = BuildContent();
    }

    public UIElement Content { get; }

    public void ApplyTo(ApplicationSettings settings)
    {
        settings.Shortcuts = new Dictionary<ApplicationCommand, ShortcutGesture>(_shortcuts);
    }

    private UIElement BuildContent()
    {
        var layout = new Grid
        {
            MinWidth = 620,
            RowSpacing = 12
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        layout.Children.Add(PlaybackSettingsPage.CreateHeading(
            "Keyboard",
            "Choose an action and press the key combination you want to use."));

        var introduction = new TextBlock { Text = "Select an action below to capture a new shortcut." };
        Grid.SetRow(introduction, 1);
        layout.Children.Add(introduction);

        var commandList = new StackPanel { Spacing = 4 };
        foreach (var command in Enum.GetValues<ApplicationCommand>())
        {
            commandList.Children.Add(CreateCommandButton(command));
        }

        var scrollViewer = new ScrollViewer
        {
            Content = commandList,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Auto
        };
        Grid.SetRow(scrollViewer, 2);
        layout.Children.Add(scrollViewer);

        Grid.SetRow(_captureMessage, 3);
        layout.Children.Add(_captureMessage);

        var footer = new Grid { ColumnSpacing = 12 };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var resetButton = new Button { Content = "Reset to defaults" };
        resetButton.Click += OnResetDefaults;
        footer.Children.Add(resetButton);
        Grid.SetColumn(_warningText, 1);
        footer.Children.Add(_warningText);
        Grid.SetRow(footer, 4);
        layout.Children.Add(footer);

        return layout;
    }

    private Button CreateCommandButton(ApplicationCommand command)
    {
        var row = new Grid { ColumnSpacing = 16 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });

        row.Children.Add(new TextBlock
        {
            Text = ShortcutDefaults.Label(command),
            VerticalAlignment = VerticalAlignment.Center
        });

        var gestureLabel = new TextBlock
        {
            Text = _shortcuts[command].DisplayText,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        _gestureLabels[command] = gestureLabel;
        Grid.SetColumn(gestureLabel, 1);
        row.Children.Add(gestureLabel);

        var button = new Button
        {
            Content = row,
            Tag = command,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 8, 12, 8)
        };
        button.Click += OnCommandClicked;
        return button;
    }

    private void OnCommandClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ApplicationCommand command }) return;

        _capturingCommand = command;
        _warningText.Text = string.Empty;
        _captureMessage.Text = $"Press the new shortcut for {ShortcutDefaults.Label(command)}. Press Esc to cancel.";
    }

    public void HandlePreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (_capturingCommand is not ApplicationCommand command) return;
        if (KeyboardGestureFactory.IsModifier(e.Key)) return;

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _capturingCommand = null;
            _captureMessage.Text = "Shortcut change cancelled.";
            e.Handled = true;
            return;
        }

        var gesture = KeyboardGestureFactory.Create(e.Key);
        var duplicate = _shortcuts.FirstOrDefault(pair => pair.Key != command && pair.Value == gesture);
        if (!duplicate.Equals(default(KeyValuePair<ApplicationCommand, ShortcutGesture>)))
        {
            var answer = MessageBox(
                _ownerWindowHandle,
                $"{gesture.DisplayText} is assigned to {ShortcutDefaults.Label(duplicate.Key)}.\n\n" +
                $"Move it to {ShortcutDefaults.Label(command)} instead? The previous action will become Unassigned.",
                "Replace shortcut?",
                MessageBoxYesNo | MessageBoxIconQuestion | MessageBoxDefaultButton2);
            if (answer != MessageBoxResultYes)
            {
                _warningText.Text = "Shortcut was not changed.";
                _captureMessage.Text = "Press another shortcut, or Esc to cancel.";
                e.Handled = true;
                return;
            }

            _shortcuts[duplicate.Key] = ShortcutGesture.Unassigned;
            _gestureLabels[duplicate.Key].Text = ShortcutGesture.Unassigned.DisplayText;
        }

        _shortcuts[command] = gesture;
        _gestureLabels[command].Text = gesture.DisplayText;
        _capturingCommand = null;
        _warningText.Text = string.Empty;
        _captureMessage.Text = $"{ShortcutDefaults.Label(command)}: {gesture.DisplayText}";
        e.Handled = true;
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        var defaults = ShortcutDefaults.Create();
        foreach (var command in Enum.GetValues<ApplicationCommand>())
        {
            _shortcuts[command] = defaults[command];
            _gestureLabels[command].Text = defaults[command].DisplayText;
        }

        _capturingCommand = null;
        _warningText.Text = string.Empty;
        _captureMessage.Text = "Defaults restored. Select Save to apply them.";
    }

    public bool TryValidate()
    {
        var duplicate = _shortcuts
            .Where(pair => pair.Value.IsAssigned)
            .GroupBy(pair => pair.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            _warningText.Text = $"{duplicate.Key.DisplayText} is assigned more than once.";
            return false;
        }

        return true;
    }

    private const uint MessageBoxYesNo = 0x00000004;
    private const uint MessageBoxIconQuestion = 0x00000020;
    private const uint MessageBoxDefaultButton2 = 0x00000100;
    private const int MessageBoxResultYes = 6;

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(nint windowHandle, string text, string caption, uint type);
}
