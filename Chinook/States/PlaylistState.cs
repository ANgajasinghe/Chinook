using Chinook.Models;

namespace Chinook.States;

public class PlaylistState
{
    public List<Playlist> Playlists { get; set; } = new();
    public event Action? OnChange;
    
    public void SetPlaylists(List<Playlist> playlists)
    {
        Playlists = playlists;
        OnChange?.Invoke();
    }
    
    public void AddPlaylist(Playlist playlist)
    {
        Playlists.Add(playlist);
        OnChange?.Invoke();
    }
    
    public void RemovePlaylist(Playlist playlist)
    {
        Playlists.Remove(playlist);
        OnChange?.Invoke();
    }
}