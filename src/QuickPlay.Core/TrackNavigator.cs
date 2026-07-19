namespace QuickPlay.Core;

public sealed class TrackNavigator
{
    private IReadOnlyList<Track> _tracks = [];
    private int _currentIndex = -1;
    public IReadOnlyList<Track> Tracks => _tracks;
    public Track? Current => _currentIndex >= 0 ? _tracks[_currentIndex] : null;

    public void SetTracks(IEnumerable<Track> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        _tracks = tracks.ToArray();
        _currentIndex = _tracks.Count == 0 ? -1 : 0;
    }

    public Track? MoveNext() => MoveBy(1);
    public Track? MovePrevious() => MoveBy(-1);

    public Track? Select(int index)
    {
        if (index < 0 || index >= _tracks.Count) return null;
        _currentIndex = index;
        return Current;
    }

    public Track? RemoveCurrent()
    {
        if (_currentIndex < 0) return null;
        var remaining = _tracks.Where((_, index) => index != _currentIndex).ToArray();
        var nextIndex = remaining.Length == 0 ? -1 : Math.Min(_currentIndex, remaining.Length - 1);
        _tracks = remaining;
        _currentIndex = nextIndex;
        return Current;
    }

    private Track? MoveBy(int offset)
    {
        if (_tracks.Count == 0) return null;
        var candidate = _currentIndex + offset;
        if (candidate < 0 || candidate >= _tracks.Count) return null;
        _currentIndex = candidate;
        return Current;
    }
}
