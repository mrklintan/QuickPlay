namespace QuickPlay.Core;

public sealed class PlaylistSaveCoordinator
{
    private readonly IPlaylistStore _store;
    private readonly object _sync = new();
    private PlaylistState? _lastSaved;
    private PlaylistState? _saving;
    private PlaylistState? _pending;
    private Task _worker = Task.CompletedTask;
    private bool _workerRunning;

    public PlaylistSaveCoordinator(IPlaylistStore store, PlaylistState? persistedPlaylist = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _lastSaved = persistedPlaylist;
    }

    public Task QueueSave(PlaylistState playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        lock (_sync)
        {
            if (Equivalent(playlist, _pending) ||
                (_pending is null && Equivalent(playlist, _saving)) ||
                (_pending is null && _saving is null && Equivalent(playlist, _lastSaved)))
                return _worker;

            _pending = playlist;
            if (!_workerRunning)
            {
                _workerRunning = true;
                _worker = Task.Run(SavePending);
            }
            return _worker;
        }
    }

    public Task FlushAsync()
    {
        lock (_sync) return _worker;
    }

    private void SavePending()
    {
        Exception? failure = null;
        while (true)
        {
            PlaylistState? playlist;
            lock (_sync)
            {
                playlist = _pending;
                _pending = null;
                _saving = playlist;
            }

            if (playlist is not null)
            {
                try
                {
                    _store.Save(playlist);
                    lock (_sync) _lastSaved = playlist;
                    failure = null;
                }
                catch (Exception exception) { failure = exception; }
            }

            lock (_sync)
            {
                _saving = null;
                if (_pending is not null) continue;
                _workerRunning = false;
                break;
            }
        }

        if (failure is not null)
            throw new IOException("The playlist could not be saved.", failure);
    }

    private static bool Equivalent(PlaylistState? left, PlaylistState? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null ||
            !string.Equals(left.RootFolder, right.RootFolder, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(left.CurrentTrack, right.CurrentTrack, StringComparison.OrdinalIgnoreCase) ||
            left.Tracks.Count != right.Tracks.Count)
            return false;

        for (var index = 0; index < left.Tracks.Count; index++)
        {
            var leftTrack = left.Tracks[index];
            var rightTrack = right.Tracks[index];
            if (leftTrack.Played != rightTrack.Played ||
                !string.Equals(leftTrack.Path, rightTrack.Path, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
