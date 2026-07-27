namespace QuickPlay.Core;

public interface IPlaylistStore
{
    PlaylistState Load();
    void Save(PlaylistState playlist);
}
