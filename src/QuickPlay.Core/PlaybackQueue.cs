namespace QuickPlay.Core;

public sealed class PlaybackQueue
{
    private readonly List<Track> _remaining = [];
    private readonly HashSet<Track> _completed = [];

    public Track? Current { get; private set; }
    public IReadOnlyList<Track> Tracks => VisibleTracks;
    public IReadOnlyList<Track> UpNext => [.. _remaining];
    public IReadOnlyCollection<Track> Completed => _completed;
    public IReadOnlyCollection<Track> Played => _completed;

    public IReadOnlyList<Track> VisibleTracks => Current is null
        ? [.. _remaining]
        : [Current, .. _remaining];

    public void SetTracks(
        IEnumerable<Track> tracks,
        Track? current = null,
        IEnumerable<Track>? completed = null)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        var uniqueTracks = DistinctByReference(tracks);
        if (current is not null && !uniqueTracks.Contains(current))
            throw new ArgumentException("The current track must be part of the playlist.", nameof(current));

        Current = current ?? uniqueTracks.FirstOrDefault();
        _remaining.Clear();
        _remaining.AddRange(uniqueTracks.Where(track => !ReferenceEquals(track, Current)));
        _completed.Clear();
        if (completed is not null)
        {
            foreach (var track in completed)
                if (uniqueTracks.Contains(track)) _completed.Add(track);
        }
    }

    public bool IsCompleted(Track track) => _completed.Contains(track);

    public void MarkCurrentCompleted()
    {
        if (Current is not null) _completed.Add(Current);
    }

    public void MarkCompleted(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (VisibleTracks.Contains(track)) _completed.Add(track);
    }

    public void MarkUnplayed(Track track) => _completed.Remove(track);

    public void MarkAllUnplayed() => _completed.Clear();

    public Track? MoveNext(bool removeCompletedTracks = true) =>
        MoveTo(_remaining.FirstOrDefault(track => !_completed.Contains(track)), removeCompletedTracks);

    public Track? MovePrevious(bool removeCompletedTracks = true) =>
        MoveTo(_remaining.LastOrDefault(track => !_completed.Contains(track)), removeCompletedTracks);

    public Track? Select(Track track, bool removeCompletedTracks = true)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (ReferenceEquals(Current, track)) return Current;
        return _remaining.Contains(track) ? MoveTo(track, removeCompletedTracks) : null;
    }

    public void ReorderTracks(IEnumerable<Track> orderedTracks)
    {
        ArgumentNullException.ThrowIfNull(orderedTracks);
        var ordered = orderedTracks.ToArray();
        var all = VisibleTracks;
        if (ordered.Length != all.Count || ordered.Distinct().Count() != ordered.Length ||
            ordered.Any(track => !all.Contains(track)))
            throw new ArgumentException(
                "The reordered tracks must contain every playlist track exactly once.",
                nameof(orderedTracks));

        _remaining.Clear();
        _remaining.AddRange(ordered.Where(track => !ReferenceEquals(track, Current)));
    }

    public Track? RemoveCurrent()
    {
        if (Current is null) return null;
        _completed.Remove(Current);
        Current = _remaining.FirstOrDefault();
        if (Current is not null) _remaining.Remove(Current);
        return Current;
    }

    private Track? MoveTo(Track? target, bool removeCompletedTracks)
    {
        if (target is null) return null;
        var previous = Current;
        _remaining.Remove(target);
        if (previous is not null)
        {
            if (removeCompletedTracks && _completed.Contains(previous))
                _completed.Remove(previous);
            else
                _remaining.Add(previous);
        }
        Current = target;
        return Current;
    }

    private static IReadOnlyList<Track> DistinctByReference(IEnumerable<Track> tracks)
    {
        var unique = new List<Track>();
        var seen = new HashSet<Track>();
        foreach (var track in tracks)
        {
            if (track is not null && seen.Add(track)) unique.Add(track);
        }
        return unique;
    }
}
