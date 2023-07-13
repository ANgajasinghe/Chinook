using Chinook.ClientModels;
using Chinook.Constants;
using Chinook.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services;

public class ArtistService
{
    private readonly ChinookContext _context;
    public ArtistService(ChinookContext context)
    {
        _context = context;
    }
    
    public async Task<Artist?> GetArtistByIdAsync(long artistId)
    {
        return await _context.Artists.SingleOrDefaultAsync(a => a.ArtistId == artistId);
    }
    
    public async Task<List<Artist>> GetArtistsAsync()
    {
        return await _context.Artists.Include(x=>x.Albums).ToListAsync();
    }
    
    public async Task<List<Album>> GetAlbumsByArtistIdAsync(long artistId)
    {
        return _context.Albums.Where(a => a.ArtistId == artistId).ToList();
    }

    public async Task<List<PlaylistTrack>> GetTracksByArtistIdAsync(long artistId, string? currentUserId)
    {
        return await _context.Tracks.Where(a => a.Album != null && a.Album.ArtistId == artistId)
            .Include(a => a.Album)
            .Select(t => new PlaylistTrack()
            {
                AlbumTitle = t.Album == null ? "-" : t.Album.Title,
                TrackId = t.TrackId,
                TrackName = t.Name,
                IsFavorite = t.Playlists.Any(p =>
                    p.UserPlaylists.Any(up => up.UserId == currentUserId && up.Playlist.Name == PlaylistConst.Favorites))
            })
            .ToListAsync();
    }
}