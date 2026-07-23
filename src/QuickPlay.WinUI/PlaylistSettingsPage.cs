using QuickPlay.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace QuickPlay.WinUI;

public sealed class PlaylistSettingsPage
{
    private readonly ListView _activeList = new() { SelectionMode = ListViewSelectionMode.Single };
    private readonly ListView _availableList = new() { SelectionMode = ListViewSelectionMode.Single };
    private readonly Button _addButton = new() { Content = "Add" };
    private readonly Button _removeButton = new() { Content = "Remove" };
    private readonly Button _moveUpButton = new() { Content = "Move Up" };
    private readonly Button _moveDownButton = new() { Content = "Move Down" };
    private readonly List<PlaylistColumn> _activeColumns;
    private bool _resetWidths;

    public PlaylistSettingsPage(PlaylistLayoutSettings layout)
    {
        layout.EnsureValid();
        _activeColumns = [.. layout.Columns];
        Content = BuildContent();

        _activeList.SelectionChanged += OnSelectionChanged;
        _availableList.SelectionChanged += OnSelectionChanged;
        _addButton.Click += OnAdd;
        _removeButton.Click += OnRemove;
        _moveUpButton.Click += OnMoveUp;
        _moveDownButton.Click += OnMoveDown;
        RefreshLists();
    }

    public UIElement Content { get; }

    public void ApplyTo(PlaylistLayoutSettings layout)
    {
        layout.Columns = [.. _activeColumns];
        if (_resetWidths) layout.ColumnWidths = PlaylistLayoutSettings.CreateDefaultWidths();
        layout.EnsureValid();
    }

    private UIElement BuildContent()
    {
        var root = new StackPanel { MinWidth = 680, Spacing = 18 };
        root.Children.Add(PlaybackSettingsPage.CreateHeading(
            "Playlist",
            "Choose which metadata columns are visible and arrange their order."));

        var layout = new Grid { Width = 680, RowSpacing = 12, ColumnSpacing = 18 };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(280) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var activeHeading = new TextBlock { Text = "Active columns", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        var availableHeading = new TextBlock { Text = "Available columns", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        Grid.SetColumn(availableHeading, 2);
        layout.Children.Add(activeHeading);
        layout.Children.Add(availableHeading);

        Grid.SetRow(_activeList, 1);
        layout.Children.Add(_activeList);
        Grid.SetRow(_availableList, 1);
        Grid.SetColumn(_availableList, 2);
        layout.Children.Add(_availableList);

        var actionButtons = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _addButton, _removeButton, _moveUpButton, _moveDownButton }
        };
        Grid.SetRow(actionButtons, 1);
        Grid.SetColumn(actionButtons, 1);
        layout.Children.Add(actionButtons);

        var resetButton = new Button { Content = "Reset to Defaults" };
        resetButton.Click += OnReset;
        Grid.SetRow(resetButton, 2);
        layout.Children.Add(resetButton);

        var help = new TextBlock
        {
            Text = "Artist and Title are fixed. Optional columns can be added, removed, and reordered after them.",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 310
        };
        Grid.SetRow(help, 2);
        Grid.SetColumn(help, 1);
        Grid.SetColumnSpan(help, 2);
        layout.Children.Add(help);
        root.Children.Add(layout);
        return root;
    }

    private void RefreshLists(PlaylistColumn? selectActive = null, PlaylistColumn? selectAvailable = null)
    {
        _activeList.Items.Clear();
        foreach (var column in _activeColumns)
        {
            var definition = PlaylistColumns.Get(column);
            var item = new ListViewItem
            {
                Content = definition.IsFixed ? $"{definition.DisplayName}  [Fixed]" : definition.DisplayName,
                Tag = column,
                IsEnabled = !definition.IsFixed,
                MinHeight = 34,
                Padding = new Thickness(8, 4, 8, 4)
            };
            _activeList.Items.Add(item);
            if (column == selectActive) _activeList.SelectedItem = item;
        }

        _availableList.Items.Clear();
        foreach (var definition in PlaylistColumns.All.Where(definition =>
                     !definition.IsFixed && !_activeColumns.Contains(definition.Column)))
        {
            var item = new ListViewItem
            {
                Content = definition.DisplayName,
                Tag = definition.Column,
                MinHeight = 34,
                Padding = new Thickness(8, 4, 8, 4)
            };
            _availableList.Items.Add(item);
            if (definition.Column == selectAvailable) _availableList.SelectedItem = item;
        }
        UpdateButtons();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(sender, _activeList) && _activeList.SelectedItem is not null)
            _availableList.SelectedItem = null;
        if (ReferenceEquals(sender, _availableList) && _availableList.SelectedItem is not null)
            _activeList.SelectedItem = null;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var activeIndex = SelectedActiveIndex();
        _addButton.IsEnabled = SelectedColumn(_availableList) is not null;
        _removeButton.IsEnabled = activeIndex >= 2;
        _moveUpButton.IsEnabled = activeIndex > 2;
        _moveDownButton.IsEnabled = activeIndex >= 2 && activeIndex < _activeColumns.Count - 1;
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        if (SelectedColumn(_availableList) is not PlaylistColumn column) return;
        _activeColumns.Add(column);
        RefreshLists(selectActive: column);
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        var index = SelectedActiveIndex();
        if (index < 2) return;
        var column = _activeColumns[index];
        _activeColumns.RemoveAt(index);
        RefreshLists(selectAvailable: column);
    }

    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        var index = SelectedActiveIndex();
        if (index <= 2) return;
        var column = _activeColumns[index];
        (_activeColumns[index - 1], _activeColumns[index]) = (_activeColumns[index], _activeColumns[index - 1]);
        RefreshLists(selectActive: column);
    }

    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        var index = SelectedActiveIndex();
        if (index < 2 || index >= _activeColumns.Count - 1) return;
        var column = _activeColumns[index];
        (_activeColumns[index + 1], _activeColumns[index]) = (_activeColumns[index], _activeColumns[index + 1]);
        RefreshLists(selectActive: column);
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        _activeColumns.Clear();
        _activeColumns.AddRange(PlaylistColumns.DefaultOrder);
        _resetWidths = true;
        RefreshLists();
    }

    private int SelectedActiveIndex()
    {
        if (SelectedColumn(_activeList) is not PlaylistColumn column) return -1;
        return _activeColumns.IndexOf(column);
    }

    private static PlaylistColumn? SelectedColumn(ListView list) =>
        list.SelectedItem is ListViewItem { Tag: PlaylistColumn column } ? column : null;
}
