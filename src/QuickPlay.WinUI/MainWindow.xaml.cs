using System.Diagnostics;
using System.Collections.ObjectModel;
using QuickPlay.Audio;
using QuickPlay.Core;
using QuickPlay.Waveform;
using QuickPlay.WinUI.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace QuickPlay.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly TrackCatalog _catalog = new();
    private readonly PlaybackQueue _queue = new();
    private readonly FolderNavigator _folderNavigator = new();
    private readonly ITrackMetadataReader _metadataReader = new TagLibTrackMetadataReader();
    private readonly ISettingsStore _settingsStore;
    private readonly ApplicationSettings _settings;
    private readonly ShortcutManager _shortcutManager;
    private readonly ClipboardFileService _clipboardService = new();
    private readonly ShellFileService _shellFileService = new();
    private readonly ExplorerFileSelector _explorerFileSelector = new();
    private readonly PlaylistRestoreLogWriter _restoreLogWriter = new();
    private readonly HttpClient _updateHttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly GitHubUpdateService _updateService;
    private readonly AudioPlayer _player;
    private readonly PlaybackController _playback;
    private readonly IWaveformAnalyzer _waveformAnalyzer = new BassWaveformAnalyzer();
    private readonly DispatcherTimer _positionTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly Dictionary<Track, TrackListItemViewModel> _itemsByTrack = [];
    private readonly Dictionary<PlaylistColumn, FrameworkElement> _playlistHeaderContainers = [];
    private readonly Dictionary<PlaylistColumn, Button> _playlistHeaderButtons = [];
    private CancellationTokenSource? _waveformCancellation;
    private CancellationTokenSource? _metadataCancellation;
    private WaveformData? _waveform;
    private Line? _playheadLine;
    // This is always the explicitly opened folder, never a recursively discovered subfolder.
    // Manual and automatic sibling navigation both use its parent as their sibling scope.
    private string? _currentFolderPath;
    private bool _updatingSelection;
    private bool _dialogOpen;
    private ScrollViewer? _trackListScrollViewer;
    private PlaylistColumn? _resizingColumn;
    private double _resizeStartWidth;
    private TimeSpan _currentPlayedTime;
    private DateTimeOffset? _playbackClockStartedAt;
    private bool _playlistReadyForNavigation;
    private bool _naturalEndHandled;
    private bool _pausedByUser;
    private bool _updateCheckInProgress;

    public ObservableCollection<TrackListItemViewModel> Tracks { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "QuickPlay.ico");
        if (File.Exists(iconPath)) AppWindow.SetIcon(iconPath);
        RootGrid.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnKeyDown), true);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsPath = System.IO.Path.Combine(localAppData, "QuickPlay", "settings.json");
        MigrateLegacySettings(
            System.IO.Path.Combine(localAppData, "DJPlayer", "settings.json"),
            settingsPath);
        _settingsStore = new JsonSettingsStore(settingsPath);
        _settings = _settingsStore.Load();
        _shortcutManager = new ShortcutManager(_settings);
        _updateService = new GitHubUpdateService(_updateHttpClient);
        _player = new AudioPlayer(new BassAudioBackend());
        _playback = new PlaybackController(_queue, _player, _settings);
        _positionTimer.Tick += OnPositionTimerTick;
        Closed += OnClosed;
    }

    private static void MigrateLegacySettings(string legacyPath, string newPath)
    {
        if (File.Exists(newPath) || !File.Exists(legacyPath)) return;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newPath)!);
            File.Copy(legacyPath, newPath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateMenuShortcutLabels();
        ApplyPlaylistLayout(reorderQueue: false);
        _positionTimer.Start();
        RootGrid.Focus(FocusState.Programmatic);
        await RestoreSavedPlaylistAsync();
    }

    private async Task RestoreSavedPlaylistAsync()
    {
        var session = _settings.PlaylistSession;
        if (!session.HasSavedPlaylist) return;

        var folderPath = session.FolderPath!;
        var savedCount = session.PlaylistFiles.Count;
        if (!Directory.Exists(folderPath))
        {
            var missingRootFailures = session.PlaylistFiles
                .Select(path => new PlaylistRestoreFailure(path, "The restore root folder does not exist."))
                .ToArray();
            ResetToDefaultState();
            await ShowRestoreWarningAsync(missingRootFailures, folderPath, useDefault: true);
            return;
        }

        var loadedCount = await OpenFolderAsync(
            folderPath,
            autoPlay: false,
            session.PlaylistFiles,
            session.CurrentTrackPath,
            session.CompletedFiles);
        var missingCount = Math.Max(0, savedCount - loadedCount);
        var restoredPaths = _itemsByTrack.Keys
            .Select(track => track.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var failures = session.PlaylistFiles
            .Where(path => !restoredPaths.Contains(path))
            .Select(path => new PlaylistRestoreFailure(
                path,
                File.Exists(path)
                    ? "The file is not a supported audio file under the restore root."
                    : "File does not exist."))
            .ToArray();
        if (loadedCount == 0)
        {
            ResetToDefaultState();
            await ShowRestoreWarningAsync(failures, folderPath, useDefault: true);
        }
        else if (missingCount > 0)
        {
            await ShowRestoreWarningAsync(failures, folderPath, useDefault: false);
        }
    }

    private async Task ShowRestoreWarningAsync(
        IReadOnlyCollection<PlaylistRestoreFailure> failures,
        string folderPath,
        bool useDefault)
    {
        var fileCount = failures.Count;
        string? logPath = null;
        string? logError = null;
        try { logPath = _restoreLogWriter.Write(folderPath, failures); }
        catch (Exception exception) { logError = exception.Message; }
        var suffix = useDefault
            ? "QuickPlay will continue with an empty playlist and the default layout."
            : "The available tracks were restored.";
        var logStatus = logPath is not null
            ? $"\n\nA detailed report was saved to:\n{logPath}"
            : $"\n\nA detailed report could not be created{(string.IsNullOrWhiteSpace(logError) ? "." : $": {logError}")}";
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "Playlist could not be fully restored",
            PrimaryButtonText = logPath is null ? string.Empty : "Open Log",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            Content = new TextBlock
            {
                Text = $"Could not load {fileCount} file{(fileCount == 1 ? string.Empty : "s")} from folder:\n{folderPath}\n\n{suffix}{logStatus}",
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 560
            }
        };
        _dialogOpen = true;
        try
        {
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && logPath is not null)
            {
                try { Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true }); }
                catch (Exception exception) { StatusText.Text = $"Could not open the restore log: {exception.Message}"; }
            }
        }
        finally
        {
            _dialogOpen = false;
            FocusPlaybackSurface();
        }
    }

    private void ResetToDefaultState()
    {
        _player.Stop();
        _currentFolderPath = null;
        _itemsByTrack.Clear();
        _queue.SetTracks([]);
        _playlistReadyForNavigation = true;
        _naturalEndHandled = true;
        _settings.PlaylistSession.Clear();
        _settings.PlaylistLayout.ResetColumns();
        _settings.PlaylistLayout.ColumnWidths = PlaylistLayoutSettings.CreateDefaultWidths();
        ApplyPlaylistLayout(reorderQueue: false);
        CurrentFolderText.Text = "—";
        ToolTipService.SetToolTip(CurrentFolderText, null);
        _waveform = null;
        DrawWaveform();
        ResetNowPlaying();
        UpdateTimeDisplay();
        StatusText.Text = "Ready. Drop a music folder anywhere in this window.";
    }

    private async void OnChooseFolder(object sender, RoutedEventArgs e)
    {
        await ChooseFolderAsync();
    }

    private async Task ChooseFolderAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null) await OpenFolderAsync(folder.Path);
        }
        finally
        {
            FocusPlaybackSurface();
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Open this music folder";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            var items = await e.DataView.GetStorageItemsAsync();
            var folder = items.OfType<StorageFolder>().FirstOrDefault();
            if (folder is null)
            {
                StatusText.Text = "Drop a folder, not individual files.";
                return;
            }
            await OpenFolderAsync(folder.Path);
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void OnExit(object sender, RoutedEventArgs e) => Close();

    private async void OnClearPlaylist(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "Clear playlist?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            Content = new TextBlock
            {
                Text = "Clear the current playlist and close the current folder?",
                TextWrapping = TextWrapping.Wrap
            }
        };
        _dialogOpen = true;
        try
        {
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            ClearPlaylist();
        }
        finally
        {
            _dialogOpen = false;
            FocusPlaybackSurface();
        }
    }

    private void ClearPlaylist()
    {
        _metadataCancellation?.Cancel();
        _waveformCancellation?.Cancel();
        _player.Stop();
        StopPlaybackClock();
        _currentPlayedTime = TimeSpan.Zero;
        _currentFolderPath = null;
        _itemsByTrack.Clear();
        _queue.SetTracks([]);
        RefreshVisibleQueue();
        _playlistReadyForNavigation = true;
        _naturalEndHandled = true;
        _pausedByUser = false;
        _settings.PlaylistSession.Clear();
        CurrentFolderText.Text = "—";
        ToolTipService.SetToolTip(CurrentFolderText, null);
        _waveform = null;
        DrawWaveform();
        ResetNowPlaying();
        UpdateTimeDisplay();
        StatusText.Text = "Playlist cleared. No folder is open.";
        try { _settingsStore.Save(_settings); }
        catch (Exception exception) { StatusText.Text = $"Playlist cleared, but settings could not be saved: {exception.Message}"; }
    }

    private void OnOpenCurrentFileInExplorer(object sender, RoutedEventArgs e)
    {
        var track = _playback.CurrentTrack ?? _queue.Current;
        if (track is null)
        {
            StatusText.Text = "No current file to show in File Explorer.";
            return;
        }
        try { _explorerFileSelector.Select(track.FilePath); }
        catch (Exception exception) { StatusText.Text = $"Could not open File Explorer: {exception.Message}"; }
        FocusPlaybackSurface();
    }

    private void OnApplicationCommandMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string commandName } &&
            Enum.TryParse<ApplicationCommand>(commandName, out var command))
            ExecuteCommand(command);
    }

    private void UpdateMenuShortcutLabels()
    {
        var menuItems = new (MenuFlyoutItem Item, ApplicationCommand Command)[]
        {
            (PlayPauseMenuItem, ApplicationCommand.PlayPause),
            (PreviousTrackMenuItem, ApplicationCommand.PreviousTrack),
            (NextTrackMenuItem, ApplicationCommand.NextTrack),
            (SeekBackwardShortMenuItem, ApplicationCommand.SeekBackwardShort),
            (SeekForwardShortMenuItem, ApplicationCommand.SeekForwardShort),
            (SeekBackwardLongMenuItem, ApplicationCommand.SeekBackwardLong),
            (SeekForwardLongMenuItem, ApplicationCommand.SeekForwardLong),
            (PreviousFolderMenuItem, ApplicationCommand.PreviousFolder),
            (NextFolderMenuItem, ApplicationCommand.NextFolder),
            (CopyTrackMenuItem, ApplicationCommand.CopyCurrentTrack),
            (DeleteTrackMenuItem, ApplicationCommand.DeleteCurrentTrack)
        };
        foreach (var (item, command) in menuItems)
        {
            var gesture = _settings.Shortcuts.GetValueOrDefault(command);
            item.KeyboardAcceleratorTextOverride = gesture is { IsAssigned: true }
                ? gesture.DisplayText
                : string.Empty;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_dialogOpen || KeyboardGestureFactory.IsModifier(e.Key)) return;
        var gesture = KeyboardGestureFactory.Create(e.Key);
        if (gesture == new ShortcutGesture((int)VirtualKey.O, ShortcutModifiers.Control))
        {
            e.Handled = true;
            _ = ChooseFolderAsync();
            return;
        }
        if (IsEditableFocus() || IsMenuFocus()) return;
        var command = _shortcutManager.Resolve(gesture);
        if (command is null) return;
        e.Handled = true;
        ExecuteCommand(command.Value);
    }

    private void OnTrackSelected(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (_updatingSelection || TrackList.SelectedItem is not TrackListItemViewModel item ||
            ReferenceEquals(item.Track, _queue.Current)) return;
        if (!_playlistReadyForNavigation)
        {
            StatusText.Text = "Please wait until playlist metadata has finished loading.";
            return;
        }
        try
        {
            UpdateCurrentCompletionStatus();
            var startPosition = _playback.SelectAndPlay(item.Track, _settings.RemovePlayedTracks);
            if (startPosition is null) return;
            RefreshVisibleQueue();
            PlayAndPresent(startPosition);
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void OnTrackContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.ItemContainer is not ListViewItem container ||
            args.Item is not TrackListItemViewModel item) return;

        var selected = new MenuFlyoutItem();
        selected.Click += (_, _) =>
        {
            if (_queue.IsCompleted(item.Track))
            {
                _queue.MarkUnplayed(item.Track);
                if (ReferenceEquals(item.Track, _queue.Current)) ResetPlaybackClock();
                StatusText.Text = $"Marked {item.Track.DisplayName} as unplayed.";
            }
            else
            {
                _queue.MarkCompleted(item.Track);
                StatusText.Text = $"Marked {item.Track.DisplayName} as played.";
            }
            UpdatePlaybackStyles();
        };
        var all = new MenuFlyoutItem { Text = "Mark All as Unplayed" };
        all.Click += (_, _) =>
        {
            _queue.MarkAllUnplayed();
            ResetPlaybackClock();
            UpdatePlaybackStyles();
            StatusText.Text = "All tracks marked as unplayed.";
        };
        var menu = new MenuFlyout();
        menu.Items.Add(selected);
        menu.Items.Add(all);
        menu.Opening += (_, _) =>
        {
            selected.Text = _queue.IsCompleted(item.Track)
                ? "Mark as Unplayed"
                : "Mark as Played";
            _updatingSelection = true;
            TrackList.SelectedItem = item;
            _updatingSelection = false;
        };
        container.ContextFlyout = menu;
    }

    private async void OnGeneralSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new GeneralSettingsDialog(_settings)
        {
            XamlRoot = RootGrid.XamlRoot
        };
        _dialogOpen = true;
        try
        {
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            dialog.ApplyTo(_settings);
            _settingsStore.Save(_settings);
            StatusText.Text = "Settings saved.";
            RootGrid.Focus(FocusState.Programmatic);
        }
        finally
        {
            _dialogOpen = false;
            FocusPlaybackSurface();
        }
    }

    private async void OnShortcutSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new ShortcutSettingsDialog(_settings, WindowNative.GetWindowHandle(this))
        {
            XamlRoot = RootGrid.XamlRoot
        };
        _dialogOpen = true;
        try
        {
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            dialog.ApplyTo(_settings);
            _settingsStore.Save(_settings);
            UpdateMenuShortcutLabels();
            StatusText.Text = "Keyboard shortcuts saved.";
            RootGrid.Focus(FocusState.Programmatic);
        }
        finally
        {
            _dialogOpen = false;
            FocusPlaybackSurface();
        }
    }

    private async void OnPlaylistColumnsSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new PlaylistColumnsDialog(_settings.PlaylistLayout)
        {
            XamlRoot = RootGrid.XamlRoot
        };
        _dialogOpen = true;
        try
        {
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            dialog.ApplyTo(_settings.PlaylistLayout);
            _settingsStore.Save(_settings);
            ApplyPlaylistLayout(reorderQueue: true);
            StatusText.Text = "Playlist columns saved.";
            RootGrid.Focus(FocusState.Programmatic);
        }
        finally
        {
            _dialogOpen = false;
            FocusPlaybackSurface();
        }
    }

    private async void OnAbout(object sender, RoutedEventArgs e)
    {
        var version = typeof(MainWindow).Assembly.GetName().Version;
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "About QuickPlay",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "QuickPlay", FontSize = 22 },
                    new TextBlock { Text = $"Version {version?.ToString(4) ?? "1.2.0.0"}" },
                    new TextBlock
                    {
                        Text = "Fast Windows audio player for auditioning DJ and music libraries.",
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 420
                    }
                }
            }
        };
        _dialogOpen = true;
        try { await dialog.ShowAsync(); }
        finally
        {
            _dialogOpen = false;
            FocusPlaybackSurface();
        }
    }

    private async void PlayAndPresent(TimeSpan? startPosition)
    {
        var track = _queue.Current;
        if (startPosition is null || track is null) return;
        var item = FindItem(track);
        _updatingSelection = true;
        TrackList.SelectedItem = item;
        if (item is not null) TrackList.ScrollIntoView(item);
        _updatingSelection = false;
        UpdateNowPlaying(item);
        UpdatePlaybackStyles();
        _naturalEndHandled = false;
        _pausedByUser = false;
        StatusText.Text = $"Playing {track.DisplayName} from {FormatPosition(startPosition.Value)}";
        ResetPlaybackClock();
        UpdateTimeDisplay();

        _waveformCancellation?.Cancel();
        _waveformCancellation?.Dispose();
        _waveformCancellation = new CancellationTokenSource();
        try
        {
            var waveform = await _waveformAnalyzer.AnalyzeAsync(track.FilePath, 300, _waveformCancellation.Token);
            if (!ReferenceEquals(track, _queue.Current)) return;
            _waveform = waveform;
            DrawWaveform();
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            StatusText.Text = $"Playback started; waveform unavailable: {exception.Message}";
        }
    }

    private void OnWaveformPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_playback.Duration <= TimeSpan.Zero || WaveformCanvas.ActualWidth <= 0) return;
        var point = e.GetCurrentPoint(WaveformCanvas).Position;
        var position = _playback.SeekToFraction(Math.Clamp(point.X / WaveformCanvas.ActualWidth, 0, 1));
        if (position is not null)
        {
            StatusText.Text = $"Playing from {FormatPosition(position.Value)}";
            UpdateTimeDisplay();
        }
        e.Handled = true;
    }

    private void OnWaveformSizeChanged(object sender, SizeChangedEventArgs e) => DrawWaveform();

    private void DrawWaveform()
    {
        WaveformCanvas.Children.Clear();
        var width = Math.Max(1, WaveformCanvas.ActualWidth);
        var height = Math.Max(1, WaveformCanvas.ActualHeight);
        if (_waveform is not null && _waveform.Peaks.Count > 0)
        {
            var center = height / 2;
            var points = new PointCollection();
            for (var index = 0; index < _waveform.Peaks.Count; index++)
            {
                var x = index * width / Math.Max(1, _waveform.Peaks.Count - 1);
                points.Add(new Windows.Foundation.Point(x, center - (_waveform.Peaks[index] * center)));
            }
            for (var index = _waveform.Peaks.Count - 1; index >= 0; index--)
            {
                var x = index * width / Math.Max(1, _waveform.Peaks.Count - 1);
                points.Add(new Windows.Foundation.Point(x, center + (_waveform.Peaks[index] * center)));
            }
            WaveformCanvas.Children.Add(new Polygon
            {
                Points = points,
                Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 153, 255))
            });
        }
        _playheadLine = new Line
        {
            Y1 = 0,
            Y2 = height,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        WaveformCanvas.Children.Add(_playheadLine);
        UpdatePlayhead();
    }

    private async Task<int> OpenFolderAsync(
        string folderPath,
        bool autoPlay = true,
        IReadOnlyList<string>? savedPlaylist = null,
        string? savedCurrentTrack = null,
        IReadOnlyList<string>? savedCompletedFiles = null,
        TimeSpan? autoPlayStartPosition = null)
    {
        ContentDialog? loadingDialog = null;
        Task? loadingDialogTask = null;
        try
        {
            _player.Stop();
            _playlistReadyForNavigation = false;
            _naturalEndHandled = true;
            _pausedByUser = false;
            _metadataCancellation?.Cancel();
            _metadataCancellation?.Dispose();
            _metadataCancellation = new CancellationTokenSource();
            _currentFolderPath = System.IO.Path.GetFullPath(System.IO.Path.TrimEndingDirectorySeparator(folderPath));
            CurrentFolderText.Text = System.IO.Path.GetFileName(_currentFolderPath);
            ToolTipService.SetToolTip(CurrentFolderText, _currentFolderPath);
            _waveform = null;
            DrawWaveform();
            var folderTracks = _catalog.LoadFolder(_currentFolderPath);
            IReadOnlyList<Track> tracks;
            Track? restoredCurrent = null;
            if (savedPlaylist is null)
            {
                tracks = folderTracks;
            }
            else
            {
                var byPath = folderTracks.ToDictionary(track => track.FilePath, StringComparer.OrdinalIgnoreCase);
                tracks = savedPlaylist
                    .Where(byPath.ContainsKey)
                    .Select(path => byPath[path])
                    .ToArray();
                if (!string.IsNullOrWhiteSpace(savedCurrentTrack))
                    restoredCurrent = tracks.FirstOrDefault(track =>
                        string.Equals(track.FilePath, savedCurrentTrack, StringComparison.OrdinalIgnoreCase));
            }
            _itemsByTrack.Clear();
            var items = tracks.Select(track => new TrackListItemViewModel(track)).ToArray();
            foreach (var item in items)
            {
                item.ConfigureColumns(_settings.PlaylistLayout.Columns, _settings.PlaylistLayout.ColumnWidths);
                _itemsByTrack.Add(item.Track, item);
            }
            var orderedTracks = savedPlaylist is null
                ? SortItems(items).Select(item => item.Track).ToArray()
                : items.Select(item => item.Track).ToArray();
            var completedTracks = savedCompletedFiles is null
                ? []
                : tracks.Where(track => savedCompletedFiles.Contains(track.FilePath, StringComparer.OrdinalIgnoreCase));
            _queue.SetTracks(orderedTracks, restoredCurrent, completedTracks);
            RefreshVisibleQueue();
            if (tracks.Count == 0)
            {
                _playlistReadyForNavigation = true;
                ResetNowPlaying();
                StatusText.Text = "No supported audio files were found in that folder.";
                return 0;
            }
            TextBlock? loadingText = null;
            ProgressBar? loadingProgress = null;
            if (FolderLoadingPolicy.ShouldShowProgress(tracks.Count))
            {
                loadingText = new TextBlock
                {
                    Text = $"Loading track 0 of {tracks.Count}",
                    TextWrapping = TextWrapping.Wrap
                };
                loadingProgress = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = tracks.Count,
                    Value = 0,
                    IsIndeterminate = false,
                    Width = 420
                };
                loadingDialog = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "Loading tracks...",
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children = { loadingText, loadingProgress }
                    }
                };
                _dialogOpen = true;
                loadingDialogTask = ShowDialogUntilHiddenAsync(loadingDialog);
                await Task.Yield();
            }
            StatusText.Text = $"{tracks.Count} tracks found. Loading metadata…";
            if (!autoPlay)
            {
                ResetNowPlaying();
                UpdateTimeDisplay();
            }
            await LoadMetadataAsync(
                items,
                _metadataCancellation.Token,
                autoPlay,
                autoPlayStartPosition,
                completed =>
                {
                    if (loadingText is not null) loadingText.Text = $"Loading track {completed} of {tracks.Count}";
                    if (loadingProgress is not null) loadingProgress.Value = completed;
                });
            return tracks.Count;
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
            return 0;
        }
        finally
        {
            if (loadingDialog is not null)
            {
                loadingDialog.Hide();
                if (loadingDialogTask is not null)
                {
                    try { await loadingDialogTask; }
                    catch (Exception) { }
                }
                _dialogOpen = false;
                FocusPlaybackSurface();
            }
        }
    }

    private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
    {
        if (_updateCheckInProgress) return;
        _updateCheckInProgress = true;
        var closingForUpdate = false;
        try
        {
            var installedVersion = ReleaseVersion.Parse(
                typeof(MainWindow).Assembly.GetName().Version?.ToString(4) ?? "0.0.0.0");
            StatusText.Text = "Checking for updates...";
            var update = await _updateService.CheckAsync(installedVersion);
            if (!update.IsUpdateAvailable)
            {
                await ShowUpdateMessageAsync(
                    "QuickPlay is up to date",
                    $"Installed version: {installedVersion}\nLatest version: {update.AvailableVersion}");
                StatusText.Text = "QuickPlay is up to date.";
                return;
            }
            if (update.MsiAsset is null)
            {
                await ShowUpdateMessageAsync(
                    "Update unavailable",
                    $"Version {update.AvailableVersion} is available, but its x64 MSI installer could not be found.");
                StatusText.Text = "The latest release does not contain an x64 MSI installer.";
                return;
            }

            var confirmation = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "A new version of QuickPlay is available",
                PrimaryButtonText = "Download and Install",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock
                {
                    Text = $"Installed version: {installedVersion}\nAvailable version: {update.AvailableVersion}",
                    TextWrapping = TextWrapping.Wrap
                }
            };
            _dialogOpen = true;
            ContentDialogResult confirmationResult;
            try { confirmationResult = await confirmation.ShowAsync(); }
            finally { _dialogOpen = false; }
            if (confirmationResult != ContentDialogResult.Primary)
            {
                StatusText.Text = "Update cancelled.";
                return;
            }

            var msiPath = await DownloadUpdateWithProgressAsync(update.MsiAsset);
            var installerProcess = MsiInstallerLauncher.Start(msiPath);
            installerProcess.Dispose();

            var handoff = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "QuickPlay installer started",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                Content = new TextBlock
                {
                    Text = "The QuickPlay installer has been started.\n\nIt may be open behind this window. Click OK to close QuickPlay, then complete the installation in the installer window.\n\nWhen installation is finished, start QuickPlay again from the Start menu.",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 520
                }
            };
            _dialogOpen = true;
            try { await handoff.ShowAsync(); }
            finally { _dialogOpen = false; }
            closingForUpdate = true;
            Close();
        }
        catch (Exception exception)
        {
            StatusText.Text = "Update check failed.";
            await ShowUpdateMessageAsync(
                "Could not check for updates",
                $"QuickPlay could not complete the update check.\n\n{exception.Message}");
        }
        finally
        {
            _updateCheckInProgress = false;
            if (!closingForUpdate && !_dialogOpen) FocusPlaybackSurface();
        }
    }

    private async Task<string> DownloadUpdateWithProgressAsync(UpdateAsset asset)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "Downloading update...",
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = asset.Name, TextWrapping = TextWrapping.Wrap },
                    new ProgressBar { IsIndeterminate = true, Width = 420 }
                }
            }
        };
        _dialogOpen = true;
        var dialogTask = ShowDialogUntilHiddenAsync(dialog);
        await Task.Yield();
        try
        {
            StatusText.Text = $"Downloading {asset.Name}...";
            return await _updateService.DownloadMsiAsync(asset);
        }
        finally
        {
            dialog.Hide();
            try { await dialogTask; } catch (Exception) { }
            _dialogOpen = false;
        }
    }

    private async Task ShowUpdateMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = title,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 520
            }
        };
        _dialogOpen = true;
        try { await dialog.ShowAsync(); }
        finally { _dialogOpen = false; }
    }

    private static async Task ShowDialogUntilHiddenAsync(ContentDialog dialog) => await dialog.ShowAsync();

    private async Task LoadMetadataAsync(
        IReadOnlyList<TrackListItemViewModel> items,
        CancellationToken cancellationToken,
        bool autoPlay = false,
        TimeSpan? autoPlayStartPosition = null,
        Action<int>? reportProgress = null)
    {
        try
        {
            var current = _queue.Current;
            var ordered = items.OrderByDescending(item => ReferenceEquals(item.Track, current));
            var completed = 0;
            foreach (var item in ordered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var metadata = await _metadataReader.ReadAsync(item.Track.FilePath, cancellationToken);
                item.ApplyMetadata(metadata);
                if (ReferenceEquals(item.Track, _queue.Current)) UpdateNowPlaying(item);
                reportProgress?.Invoke(++completed);
            }
            SortPlaylistAndRefresh();
            _playlistReadyForNavigation = true;
            StatusText.Text = $"{items.Count} tracks ready.";
            if (autoPlay)
            {
                if (autoPlayStartPosition is TimeSpan startPosition)
                    ExecutePlayback(() => _playback.PlayCurrentFrom(startPosition));
                else
                    ExecutePlayback(_playback.PlayCurrent);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async void ExecuteCommand(ApplicationCommand command)
    {
        switch (command)
        {
            case ApplicationCommand.PlayPause:
                TogglePlayPause();
                break;
            case ApplicationCommand.PreviousTrack:
                await PlayPreviousTrackAsync();
                break;
            case ApplicationCommand.NextTrack:
                await PlayNextTrackAsync();
                break;
            case ApplicationCommand.PreviousFolder:
                await NavigateSiblingFolderAsync(next: false);
                break;
            case ApplicationCommand.NextFolder:
                await NavigateSiblingFolderAsync(next: true);
                break;
            case ApplicationCommand.SeekBackwardShort:
                SeekAndPresent(TimeSpan.FromSeconds(-_settings.ShortSeekSeconds));
                break;
            case ApplicationCommand.SeekForwardShort:
                SeekAndPresent(TimeSpan.FromSeconds(_settings.ShortSeekSeconds));
                break;
            case ApplicationCommand.SeekBackwardLong:
                SeekAndPresent(TimeSpan.FromSeconds(-_settings.LongSeekSeconds));
                break;
            case ApplicationCommand.SeekForwardLong:
                SeekAndPresent(TimeSpan.FromSeconds(_settings.LongSeekSeconds));
                break;
            case ApplicationCommand.CopyCurrentTrack:
                await CopyCurrentTrackAsync();
                break;
            case ApplicationCommand.DeleteCurrentTrack:
                DeleteCurrentTrack();
                break;
        }
    }

    private async Task CopyCurrentTrackAsync()
    {
        var track = _playback.CurrentTrack;
        if (track is null)
        {
            StatusText.Text = "No active track to copy.";
            return;
        }
        try
        {
            await _clipboardService.CopyFileAsync(track.FilePath);
            StatusText.Text = $"Copied {System.IO.Path.GetFileName(track.FilePath)} to the clipboard.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not copy track: {exception.Message}";
        }
    }

    private void DeleteCurrentTrack()
    {
        var track = _playback.CurrentTrack;
        if (track is null)
        {
            StatusText.Text = "No active track to delete.";
            return;
        }

        var position = _playback.Position;
        var wasPlaying = _playback.IsPlaying;
        var item = FindItem(track);
        _waveformCancellation?.Cancel();
        _player.Unload();

        bool deleted;
        try
        {
            deleted = _shellFileService.MoveToRecycleBinWithConfirmation(track.FilePath);
        }
        catch (Exception exception)
        {
            RestoreTrackAfterCancelledDelete(track, position, wasPlaying);
            StatusText.Text = $"Could not delete track: {exception.Message}";
            return;
        }

        if (!deleted)
        {
            RestoreTrackAfterCancelledDelete(track, position, wasPlaying);
            StatusText.Text = "Delete cancelled.";
            return;
        }

        _metadataCancellation?.Cancel();
        _queue.RemoveCurrent();
        _itemsByTrack.Remove(track);
        RefreshVisibleQueue();
        if (_queue.Current is null)
        {
            _waveform = null;
            DrawWaveform();
            ResetNowPlaying();
            UpdateTimeDisplay();
            StatusText.Text = _itemsByTrack.Count == 0
                ? "Track moved to the Recycle Bin. The folder is now empty."
                : "Track moved to the Recycle Bin. No tracks remain in Up Next.";
            return;
        }

        StatusText.Text = "Track moved to the Recycle Bin.";
        ExecutePlayback(_playback.PlayCurrent);
        _metadataCancellation?.Dispose();
        _metadataCancellation = new CancellationTokenSource();
        _ = LoadMetadataAsync(_itemsByTrack.Values.ToArray(), _metadataCancellation.Token);
    }

    private void RestoreTrackAfterCancelledDelete(Track track, TimeSpan position, bool wasPlaying)
    {
        var restoredPosition = _player.Play(track, position);
        PlayAndPresent(restoredPosition);
        if (wasPlaying) return;
        _player.TogglePause();
        _pausedByUser = true;
    }

    private void TogglePlayPause()
    {
        try
        {
            if (_playback.CurrentTrack is null && _queue.Current is not null)
            {
                if (!_playlistReadyForNavigation)
                {
                    StatusText.Text = "Please wait until playlist metadata has finished loading.";
                    return;
                }
                ExecutePlayback(_playback.PlayCurrent);
                return;
            }

            var wasPlaying = _playback.IsPlaying;
            if (wasPlaying) StopPlaybackClock();
            var isPlaying = _playback.TogglePause();
            if (isPlaying is null)
            {
                StatusText.Text = "Open a folder first.";
                return;
            }
            if (isPlaying.Value) StartPlaybackClock();
            _pausedByUser = !isPlaying.Value;
            StatusText.Text = isPlaying.Value ? "Playback resumed." : "Playback paused.";
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private async Task NavigateSiblingFolderAsync(bool next)
    {
        if (_currentFolderPath is null) return;
        try
        {
            var folder = next
                ? _folderNavigator.MoveNext(_currentFolderPath)
                : _folderNavigator.MovePrevious(_currentFolderPath);
            if (folder is not null)
            {
                await OpenFolderAsync(folder);
                return;
            }

            StatusText.Text = next
                ? "Already at the last sibling folder."
                : "Already at the first sibling folder.";
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private async Task PlayNextTrackAsync()
    {
        try
        {
            if (!EnsurePlaylistReady()) return;
            UpdateCurrentCompletionStatus();
            var startPosition = _playback.MoveNextAndPlay(_settings.RemovePlayedTracks);
            if (startPosition is not null)
            {
                RefreshVisibleQueue();
                PlayAndPresent(startPosition);
                return;
            }

            if (_currentFolderPath is null) return;
            var nextFolder = _folderNavigator.MoveNext(_currentFolderPath);
            if (nextFolder is null)
            {
                StatusText.Text = "End of the last sibling folder.";
                return;
            }

            await OpenFolderAsync(nextFolder);
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private bool EnsurePlaylistReady()
    {
        if (_playlistReadyForNavigation) return true;
        StatusText.Text = "Please wait until playlist metadata has finished loading.";
        return false;
    }

    private async void HandleNaturalEnd()
    {
        if (_naturalEndHandled || !_playlistReadyForNavigation || _playback.CurrentTrack is null)
            return;
        var action = NaturalPlaybackEndPolicy.Resolve(
                _settings.ContinuePlay,
                _playback.Position,
                _playback.Duration,
                _playback.IsPlaying,
                _pausedByUser);
        if (action == NaturalPlaybackEndAction.None) return;

        _naturalEndHandled = true;
        StopPlaybackClock();
        _queue.MarkCurrentCompleted();
        UpdatePlaybackStyles();

        if (action == NaturalPlaybackEndAction.Stop)
        {
            StatusText.Text = "Playback finished.";
            return;
        }

        try
        {
            var startPosition = _playback.MoveNextAndPlayFrom(
                _settings.ContinuePlayStartPosition,
                _settings.RemovePlayedTracks);
            if (startPosition is not null)
            {
                RefreshVisibleQueue();
                PlayAndPresent(startPosition);
                return;
            }

            if (_currentFolderPath is null) return;
            var nextFolder = _folderNavigator.MoveNext(_currentFolderPath);
            if (nextFolder is null)
            {
                StatusText.Text = "End of the last sibling folder.";
                return;
            }

            await OpenFolderAsync(
                nextFolder,
                autoPlay: true,
                autoPlayStartPosition: _settings.ContinuePlayStartPosition);
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private async Task PlayPreviousTrackAsync()
    {
        try
        {
            if (!EnsurePlaylistReady()) return;
            UpdateCurrentCompletionStatus();
            var startPosition = _playback.MovePreviousAndPlay(_settings.RemovePlayedTracks);
            if (startPosition is null)
            {
                if (_currentFolderPath is null) return;
                var previousFolder = _folderNavigator.MovePrevious(_currentFolderPath);
                if (previousFolder is not null)
                {
                    await OpenFolderAsync(previousFolder);
                    return;
                }
                StatusText.Text = "Beginning of the first sibling folder.";
                return;
            }
            RefreshVisibleQueue();
            PlayAndPresent(startPosition);
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void ExecutePlayback(Func<TimeSpan?> action)
    {
        try { PlayAndPresent(action()); }
        catch (Exception exception) { StatusText.Text = exception.Message; }
    }

    private void SeekAndPresent(TimeSpan offset)
    {
        try
        {
            var position = _playback.SeekBy(offset);
            if (position is null) return;
            StatusText.Text = $"Playing from {FormatPosition(position.Value)}";
            UpdateTimeDisplay();
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void UpdateNowPlaying(TrackListItemViewModel? item)
    {
        if (item is null) return;
        NowPlayingTitleText.Text = item.Title;
        NowPlayingArtistText.Text = string.IsNullOrWhiteSpace(item.Artist) ? "Unknown artist" : item.Artist;
        NowPlayingBpmText.Text = string.IsNullOrWhiteSpace(item.Bpm) ? "BPM —" : $"BPM {item.Bpm}";
        NowPlayingKeyText.Text = string.IsNullOrWhiteSpace(item.InitialKey) ? "Key —" : $"Key {item.InitialKey}";
        NowPlayingEnergyText.Text = string.IsNullOrWhiteSpace(item.Energy) ? "Energy —" : $"Energy {item.Energy}";
        CurrentFileNameText.Text = item.FileName;
    }

    private void ResetNowPlaying()
    {
        NowPlayingTitleText.Text = "Nothing playing";
        NowPlayingArtistText.Text = "—";
        NowPlayingBpmText.Text = "BPM —";
        NowPlayingKeyText.Text = "Key —";
        NowPlayingEnergyText.Text = "Energy —";
        CurrentFileNameText.Text = "—";
    }

    private void UpdateCurrentCompletionStatus()
    {
        var current = _queue.Current;
        if (current is null || _queue.IsCompleted(current) ||
            !PlayedTrackPolicy.ShouldMarkCompleted(_settings, CurrentPlayedTime)) return;
        _queue.MarkCurrentCompleted();
        UpdatePlaybackStyles();
    }

    private TimeSpan CurrentPlayedTime => _currentPlayedTime +
        (_playbackClockStartedAt is DateTimeOffset started && _playback.IsPlaying
            ? DateTimeOffset.UtcNow - started
            : TimeSpan.Zero);

    private void ResetPlaybackClock()
    {
        _currentPlayedTime = TimeSpan.Zero;
        _playbackClockStartedAt = _playback.IsPlaying ? DateTimeOffset.UtcNow : null;
    }

    private void StartPlaybackClock()
    {
        if (_playbackClockStartedAt is null) _playbackClockStartedAt = DateTimeOffset.UtcNow;
    }

    private void StopPlaybackClock()
    {
        if (_playbackClockStartedAt is not DateTimeOffset started) return;
        _currentPlayedTime += DateTimeOffset.UtcNow - started;
        _playbackClockStartedAt = null;
    }

    private void ApplyPlaylistLayout(bool reorderQueue)
    {
        _settings.PlaylistLayout.EnsureValid();
        BuildPlaylistHeaders();
        foreach (var item in _itemsByTrack.Values)
            item.ConfigureColumns(_settings.PlaylistLayout.Columns, _settings.PlaylistLayout.ColumnWidths);

        if (reorderQueue) SortPlaylistAndRefresh();
        else RefreshVisibleQueue();
    }

    private IEnumerable<TrackListItemViewModel> SortItems(IEnumerable<TrackListItemViewModel> items)
    {
        var layout = _settings.PlaylistLayout;
        var sorter = new PlaylistSorter(layout.SortColumn, layout.SortDirection);
        return items.OrderBy(item => item.Metadata, sorter);
    }

    private void SortPlaylistAndRefresh()
    {
        var ordered = SortItems(_queue.Tracks.Select(track => _itemsByTrack[track]))
            .Select(item => item.Track)
            .ToArray();
        _queue.ReorderTracks(ordered);
        RefreshVisibleQueue();
    }

    private void RefreshVisibleQueue()
    {
        _updatingSelection = true;
        try
        {
            Tracks.Clear();
            foreach (var track in _queue.VisibleTracks)
            {
                if (_itemsByTrack.TryGetValue(track, out var item)) Tracks.Add(item);
            }
            TrackList.SelectedItem = _queue.Current is not null ? FindItem(_queue.Current) : null;
            if (TrackList.SelectedItem is not null) TrackList.ScrollIntoView(TrackList.SelectedItem);
        }
        finally
        {
            _updatingSelection = false;
        }
        UpdatePlaybackStyles();
        ApplyPlaylistWidthsToViewport();
    }

    private void UpdatePlaybackStyles()
    {
        foreach (var (track, item) in _itemsByTrack)
            item.SetPlaybackState(
                _queue.IsCompleted(track),
                ReferenceEquals(track, _queue.Current) && _playback.CurrentTrack is not null);
    }

    private void BuildPlaylistHeaders()
    {
        PlaylistHeaderPanel.Children.Clear();
        _playlistHeaderContainers.Clear();
        _playlistHeaderButtons.Clear();
        foreach (var column in _settings.PlaylistLayout.Columns)
        {
            var width = _settings.PlaylistLayout.ColumnWidths[column];
            var container = new Grid
            {
                Width = width,
                Height = 36,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 41, 41, 41))
            };
            var button = new Button
            {
                Tag = column,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            button.Click += OnPlaylistHeaderClick;
            container.Children.Add(button);

            var resizeHandle = new Thumb
            {
                Tag = column,
                Width = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Colors.Transparent)
            };
            ToolTipService.SetToolTip(resizeHandle, "Drag to resize column");
            resizeHandle.DragStarted += OnColumnResizeStarted;
            resizeHandle.DragDelta += OnColumnResizeDelta;
            resizeHandle.DragCompleted += OnColumnResizeCompleted;
            Canvas.SetZIndex(resizeHandle, 1);
            container.Children.Add(resizeHandle);

            var divider = new Border
            {
                Width = 1,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 82, 82, 82))
            };
            container.Children.Add(divider);

            _playlistHeaderContainers[column] = container;
            _playlistHeaderButtons[column] = button;
            PlaylistHeaderPanel.Children.Add(container);
        }
        UpdatePlaylistHeaderLabels();
        ApplyPlaylistWidthsToViewport();
    }

    private void UpdatePlaylistHeaderLabels()
    {
        var layout = _settings.PlaylistLayout;
        foreach (var (column, button) in _playlistHeaderButtons)
        {
            var indicator = column == layout.SortColumn
                ? layout.SortDirection == PlaylistSortDirection.Ascending ? " ▲" : " ▼"
                : string.Empty;
            button.Content = PlaylistColumns.Get(column).DisplayName + indicator;
        }
    }

    private void OnPlaylistHeaderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PlaylistColumn column }) return;
        var layout = _settings.PlaylistLayout;
        if (layout.SortColumn == column)
        {
            layout.SortDirection = layout.SortDirection == PlaylistSortDirection.Ascending
                ? PlaylistSortDirection.Descending
                : PlaylistSortDirection.Ascending;
        }
        else
        {
            layout.SortColumn = column;
            layout.SortDirection = PlaylistSortDirection.Ascending;
        }

        UpdatePlaylistHeaderLabels();
        SortPlaylistAndRefresh();
        SavePlaylistLayout("Playlist sort saved.");
    }

    private void OnColumnResizeStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is not Thumb { Tag: PlaylistColumn column }) return;
        _resizingColumn = column;
        _resizeStartWidth = _playlistHeaderContainers.TryGetValue(column, out var header)
            ? header.ActualWidth
            : _settings.PlaylistLayout.ColumnWidths[column];
    }

    private void OnColumnResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (_resizingColumn is not PlaylistColumn column) return;
        var definition = PlaylistColumns.Get(column);
        _resizeStartWidth = Math.Clamp(_resizeStartWidth + e.HorizontalChange, definition.MinimumWidth, 1200);
        var width = _resizeStartWidth;
        _settings.PlaylistLayout.ColumnWidths[column] = width;
        ApplyPlaylistWidthsToViewport();
    }

    private void OnColumnResizeCompleted(object sender, DragCompletedEventArgs e)
    {
        FinishColumnResize();
    }

    private void FinishColumnResize()
    {
        if (_resizingColumn is null) return;
        _resizingColumn = null;
        SavePlaylistLayout("Playlist column width saved.");
    }

    private void ApplyColumnWidth(PlaylistColumn column, double width)
    {
        if (_playlistHeaderContainers.TryGetValue(column, out var header)) header.Width = width;
        foreach (var item in _itemsByTrack.Values) item.SetColumnWidth(column, width);
    }

    private void OnPlaylistViewportSizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyPlaylistWidthsToViewport();

    private void ApplyPlaylistWidthsToViewport()
    {
        var columns = _settings.PlaylistLayout.Columns;
        if (columns.Count == 0) return;
        var configuredTotal = columns.Sum(column => _settings.PlaylistLayout.ColumnWidths[column]);
        var viewportWidth = Math.Max(0, PlaylistHeaderScrollViewer.ActualWidth - 2);
        var extra = Math.Max(0, viewportWidth - configuredTotal);
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var width = _settings.PlaylistLayout.ColumnWidths[column];
            if (index == columns.Count - 1) width += extra;
            ApplyColumnWidth(column, width);
        }
    }

    private void SavePlaylistLayout(string status)
    {
        try
        {
            _settingsStore.Save(_settings);
            StatusText.Text = status;
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not save playlist layout: {exception.Message}";
        }
    }

    private void OnTrackListLoaded(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindDescendant<ScrollViewer>(TrackList);
        if (ReferenceEquals(scrollViewer, _trackListScrollViewer)) return;
        if (_trackListScrollViewer is not null) _trackListScrollViewer.ViewChanged -= OnTrackListViewChanged;
        _trackListScrollViewer = scrollViewer;
        if (_trackListScrollViewer is not null) _trackListScrollViewer.ViewChanged += OnTrackListViewChanged;
    }

    private void OnTrackListViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_trackListScrollViewer is null) return;
        PlaylistHeaderScrollViewer.ChangeView(_trackListScrollViewer.HorizontalOffset, null, null, true);
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) return match;
            var descendant = FindDescendant<T>(child);
            if (descendant is not null) return descendant;
        }
        return null;
    }

    private TrackListItemViewModel? FindItem(Track track) =>
        _itemsByTrack.GetValueOrDefault(track);

    private void OnPositionTimerTick(object? sender, object e)
    {
        UpdateCurrentCompletionStatus();
        UpdateTimeDisplay();
        HandleNaturalEnd();
    }

    private void UpdateTimeDisplay()
    {
        CurrentTimeText.Text = FormatPosition(_playback.Position);
        DurationText.Text = FormatPosition(_playback.Duration);
        UpdatePlayhead();
    }

    private void UpdatePlayhead()
    {
        if (_playheadLine is null || _playback.Duration <= TimeSpan.Zero) return;
        var fraction = Math.Clamp(_playback.Position.TotalSeconds / _playback.Duration.TotalSeconds, 0, 1);
        var x = fraction * Math.Max(1, WaveformCanvas.ActualWidth);
        _playheadLine.X1 = x;
        _playheadLine.X2 = x;
        _playheadLine.Y2 = Math.Max(1, WaveformCanvas.ActualHeight);
    }

    private bool IsEditableFocus()
    {
        var current = FocusManager.GetFocusedElement(RootGrid.XamlRoot) as DependencyObject;
        while (current is not null)
        {
            if (current is TextBox or RichEditBox or PasswordBox or AutoSuggestBox or NumberBox or ComboBox)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private bool IsMenuFocus()
    {
        var current = FocusManager.GetFocusedElement(RootGrid.XamlRoot) as DependencyObject;
        while (current is not null)
        {
            if (current is MenuBar or MenuBarItem or MenuFlyoutItem) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void FocusPlaybackSurface()
    {
        if (_playback.CurrentTrack is not null)
        {
            TrackList.Focus(FocusState.Programmatic);
            return;
        }

        RootGrid.Focus(FocusState.Programmatic);
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        SavePlaylistSession();
        _positionTimer.Stop();
        _metadataCancellation?.Cancel();
        _metadataCancellation?.Dispose();
        _waveformCancellation?.Cancel();
        _waveformCancellation?.Dispose();
        _updateHttpClient.Dispose();
        _player.Dispose();
    }

    private void SavePlaylistSession()
    {
        try
        {
            var playlistTracks = _queue.Tracks;
            if (string.IsNullOrWhiteSpace(_currentFolderPath) || playlistTracks.Count == 0)
            {
                _settings.PlaylistSession.Clear();
            }
            else
            {
                _settings.PlaylistSession.FolderPath = _currentFolderPath;
                _settings.PlaylistSession.CurrentTrackPath = _queue.Current?.FilePath;
                _settings.PlaylistSession.PlaylistFiles = playlistTracks
                    .Select(track => track.FilePath)
                    .ToList();
                _settings.PlaylistSession.CompletedFiles = _queue.Completed
                    .Select(track => track.FilePath)
                    .ToList();
            }
            _settingsStore.Save(_settings);
        }
        catch (Exception)
        {
            // The window is already closing, so there is no safe surface for an error dialog.
        }
    }

    private static string FormatPosition(TimeSpan position) =>
        $"{(int)position.TotalMinutes:00}:{position.Seconds:00}";

}
