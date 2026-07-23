using QuickPlay.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace QuickPlay.WinUI;

public enum SettingsCategory
{
    Playback,
    Playlist,
    Keyboard,
    Updates
}

public sealed class SettingsDialog
{
    private readonly ContentDialog _dialog = new();
    private readonly NavigationView _navigation = new();
    private readonly PlaybackSettingsPage _playbackPage;
    private readonly PlaylistSettingsPage _playlistPage;
    private readonly KeyboardSettingsPage _keyboardPage;
    private readonly UIElement _updatesPage;
    private readonly Dictionary<SettingsCategory, NavigationViewItem> _navigationItems = [];

    public SettingsDialog(
        ApplicationSettings settings,
        nint ownerWindowHandle,
        SettingsCategory initialCategory)
    {
        _playbackPage = new PlaybackSettingsPage(settings);
        _playlistPage = new PlaylistSettingsPage(settings.PlaylistLayout);
        _keyboardPage = new KeyboardSettingsPage(settings, ownerWindowHandle);
        _updatesPage = BuildUpdatesPage();

        _dialog.Title = "Settings";
        _dialog.PrimaryButtonText = "Save";
        _dialog.SecondaryButtonText = "Cancel";
        _dialog.DefaultButton = ContentDialogButton.Primary;
        _dialog.Resources["ContentDialogMinWidth"] = 1000d;
        _dialog.Resources["ContentDialogMaxWidth"] = 1080d;
        _dialog.Resources["ContentDialogMaxHeight"] = 760d;
        _dialog.PrimaryButtonClick += OnPrimaryButtonClick;
        _dialog.PreviewKeyDown += OnPreviewKeyDown;

        ConfigureNavigation();
        _dialog.Content = _navigation;
        Select(initialCategory);
    }

    public XamlRoot? XamlRoot
    {
        get => _dialog.XamlRoot;
        set
        {
            if (_dialog.XamlRoot is not null)
                _dialog.XamlRoot.Changed -= OnXamlRootChanged;

            _dialog.XamlRoot = value;
            if (value is null) return;

            value.Changed += OnXamlRootChanged;
            UpdateDialogSize(value);
        }
    }

    public async Task<ContentDialogResult> ShowAsync()
    {
        try
        {
            return await _dialog.ShowAsync();
        }
        finally
        {
            if (_dialog.XamlRoot is not null)
                _dialog.XamlRoot.Changed -= OnXamlRootChanged;
        }
    }

    public void ApplyTo(ApplicationSettings settings)
    {
        _playbackPage.ApplyTo(settings);
        _playlistPage.ApplyTo(settings.PlaylistLayout);
        _keyboardPage.ApplyTo(settings);
    }

    private void ConfigureNavigation()
    {
        _navigation.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        _navigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
        _navigation.IsPaneToggleButtonVisible = false;
        _navigation.IsSettingsVisible = false;
        _navigation.IsPaneOpen = true;
        _navigation.OpenPaneLength = 210;
        _navigation.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _navigation.VerticalContentAlignment = VerticalAlignment.Stretch;
        _navigation.SelectionChanged += OnSelectionChanged;

        AddNavigationItem(SettingsCategory.Playback, "Playback");
        AddNavigationItem(SettingsCategory.Playlist, "Playlist");
        AddNavigationItem(SettingsCategory.Keyboard, "Keyboard");
        AddNavigationItem(SettingsCategory.Updates, "Updates");
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) =>
        UpdateDialogSize(sender);

    private void UpdateDialogSize(XamlRoot xamlRoot)
    {
        // XamlRoot.Size uses effective pixels, so these limits remain usable at
        // every Windows display scale while leaving room for the dialog chrome.
        _dialog.Resources["ContentDialogMaxHeight"] =
            Math.Max(400d, xamlRoot.Size.Height - 40);
        _navigation.Width = Math.Clamp(xamlRoot.Size.Width - 80, 520, 940);
        _navigation.Height = Math.Clamp(xamlRoot.Size.Height - 180, 320, 590);
    }

    private void AddNavigationItem(SettingsCategory category, string title)
    {
        var item = new NavigationViewItem
        {
            Content = title,
            Tag = category
        };
        _navigationItems[category] = item;
        _navigation.MenuItems.Add(item);
    }

    private void Select(SettingsCategory category)
    {
        var item = _navigationItems[category];
        _navigation.SelectedItem = item;
        _navigation.Content = ContentFor(category);
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is NavigationViewItem { Tag: SettingsCategory category })
            sender.Content = ContentFor(category);
    }

    private UIElement ContentFor(SettingsCategory category) => category switch
    {
        SettingsCategory.Playback => _playbackPage.Content,
        SettingsCategory.Playlist => _playlistPage.Content,
        SettingsCategory.Keyboard => _keyboardPage.Content,
        SettingsCategory.Updates => _updatesPage,
        _ => _playbackPage.Content
    };

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!_playbackPage.TryValidate())
        {
            Select(SettingsCategory.Playback);
            args.Cancel = true;
            return;
        }

        if (!_keyboardPage.TryValidate())
        {
            Select(SettingsCategory.Keyboard);
            args.Cancel = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e) =>
        _keyboardPage.HandlePreviewKeyDown(e);

    private static UIElement BuildUpdatesPage()
    {
        var root = new StackPanel { MinWidth = 580, MaxWidth = 720, Spacing = 18 };
        root.Children.Add(PlaybackSettingsPage.CreateHeading(
            "Updates",
            "QuickPlay can check GitHub Releases for a newer version when you request it."));
        root.Children.Add(new Border
        {
            Padding = new Thickness(18),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 48, 48)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 72, 72, 72)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Manual update checks",
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = "Use About → Check for Updates to check for and install a newer QuickPlay release.",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "QuickPlay does not check for updates automatically.",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });
        return root;
    }
}
