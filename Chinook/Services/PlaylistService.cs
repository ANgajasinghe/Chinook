using System.Linq.Expressions;
using Chinook.ClientModels;
using Chinook.Constants;
using Chinook.Models;
using Chinook.States;
using Microsoft.EntityFrameworkCore;
using ClientModelPlayList = Chinook.ClientModels.Playlist;
using Playlist= Chinook.Models.Playlist;

namespace Chinook.Services;

public class PlaylistService
{
    private readonly ChinookContext _context;
    private readonly PlaylistState _playlistState;
    public PlaylistService(ChinookContext context, PlaylistState playlistState)
    {
        _context = context;
        _playlistState = playlistState;
    }
    
    
    public async Task<List<Playlist>> GetPlaylistsByUserIdAsync(string currentUserId)
    {
        return await _context.UserPlaylists.Include(x => x.Playlist)
            .Where(x => x.UserId == currentUserId && x.Playlist.Name != null).Select(x => x.Playlist).ToListAsync();
    }
    
    
    public async Task<Playlist> CreatePlaylistAsync(string playlistName, string currentUserId,long trackId)
    {
        var track = await _context.Tracks.FirstOrDefaultAsync(x => x.TrackId == trackId);
        
        if(track is null)
            throw new ArgumentException("Track not found");

        var playListId = await GetPlayListIdAsync();
        
        var userPlaylist = new UserPlaylist
        {
            PlaylistId = playListId,
            UserId = currentUserId,
            Playlist = new Playlist
            {
                PlaylistId = playListId,
                Name = playlistName,
                Tracks = new List<Track>{track}
            }
        };
        
        await _context.UserPlaylists.AddAsync(userPlaylist);
        await _context.SaveChangesAsync();
        _playlistState.AddPlaylist(userPlaylist.Playlist);
        return userPlaylist.Playlist;
    }


    public async Task<Playlist> AddTrackToPlayListAsync(long playlistId, string currentUserId,long trackId)
    {
        
        var playlist = await _context.Playlists.Include(x => x.UserPlaylists)
            .Where(x => x.PlaylistId == playlistId && x.UserPlaylists.Any(up => up.UserId == currentUserId))
            .FirstOrDefaultAsync();
        
        if (playlist == null)
            throw new ArgumentException("Playlist not found");
        
        var track = await _context.Tracks.FirstOrDefaultAsync(x => x.TrackId == trackId);
        
        if(track is null)
            throw new ArgumentException("Track not found");
        
        playlist.Tracks.Add(track);
        _context.Playlists.Update(playlist);
        await _context.SaveChangesAsync();
        return playlist;
    }
    
    public async Task<Track> RemoveTrackFromPlaylistAsync(long playlistId, string currentUserId,long trackId)
    {
        
        var playlist = await GetPlayListAsync(x=>x.UserId == currentUserId 
                                                 && x.Playlist.Name != PlaylistConst.Favorites 
                                                 && x.PlaylistId == playlistId);
        
        if (playlist == null)
            throw new ArgumentException("Playlist not found");
        
        var track = await _context.Tracks.FirstOrDefaultAsync(x => x.TrackId == trackId);
        
        if(track is null)
            throw new ArgumentException("Track not found");
        
        playlist.Tracks.Remove(track);
        await _context.SaveChangesAsync();
        return track;
        
    }

    public async Task<ClientModelPlayList> GetPlaylistByPlaylistIdAsync(long playlistId,string currentUserId)
    {
        return await _context.Playlists
            .Include(a => a.Tracks)
            .ThenInclude(t => t.Album)
            .ThenInclude(a => a.Artist)
            .Where(p => p.PlaylistId == playlistId)
            .Select(playlist => new ClientModelPlayList
            {
                Name = playlist.Name ?? "-",
                Tracks = playlist.Tracks.Select(track => new PlaylistTrack
                {
                    AlbumTitle = track.Album.Title,
                    ArtistName = track.Album.Artist.Name ?? "-",
                    TrackId = track.TrackId,
                    TrackName = track.Name,
                    IsFavorite = track.Playlists.Any(playlist => playlist.UserPlaylists
                        .Any(userPlaylist => userPlaylist.UserId == currentUserId 
                                             && userPlaylist.Playlist.Name == PlaylistConst.Favorites))
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
    
    public async Task<bool> FavoriteUnFavouriteTrackAsync(long trackId, string currentUserId)
    {
        var track = await _context.Tracks.SingleOrDefaultAsync(a => a.TrackId == trackId);
        
        if (track == null)
            return false;
        
        var favouritePlayList = await GetPlayListAsync(x=>x.UserId == currentUserId 
                                                          && x.Playlist.Name == PlaylistConst.Favorites);
        
        var isAlreadyFavourite = favouritePlayList?.Tracks.Any(x => x.TrackId == trackId);

        if (isAlreadyFavourite is null or false)
        {
            await MakeAsFavouriteAsync(track,favouritePlayList, currentUserId);
        }
        else
        {
            MakeAsUnFavourite(track,favouritePlayList!);
        }
        
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<long> GetPlayListIdAsync()
    {
        var lastPlayListId = await _context.Playlists.Select(x => x.PlaylistId).MaxAsync();
        return lastPlayListId + 1;
    }

    private async Task MakeAsFavouriteAsync(Track track,Playlist? playlist,string currentUserId)
    {
        if (playlist == null) // Create new favorite playlist and add track
        {
            var playListId = await GetPlayListIdAsync();
            playlist = new Playlist
            {
                PlaylistId = playListId,
                Name = PlaylistConst.Favorites,
                UserPlaylists = new List<UserPlaylist>
                {
                    new()
                    {
                        PlaylistId = playListId,
                        UserId = currentUserId
                    }
                },
                Tracks = new List<Track>{track}
            };
            await _context.Playlists.AddAsync(playlist);
            _playlistState.AddPlaylist(playlist);
        }
        else
        {
            playlist.Tracks.Add(track); // Add track to favorite playlist
        }
    }
    
    private static void MakeAsUnFavourite(Track track,Playlist playlist)
    {
        playlist.Tracks.Remove(track);
    }

    private async Task<Playlist?> GetPlayListAsync(Expression<Func<UserPlaylist,bool>> selection)
    {
        return await _context.UserPlaylists
            .Include(x => x.Playlist)
            .ThenInclude(x=>x.Tracks)
            .Where(selection)
            .Select(x=>x.Playlist)
            .FirstOrDefaultAsync();
    }
}