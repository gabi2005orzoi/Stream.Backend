using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Infrastructure.Repositories
{
    public class PlaylistRepository: IPlaylistRepository
    {
        private readonly AppDbContext db;

        public PlaylistRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task AddAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            await db.Playlists.AddAsync(playlist, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task AddTrackAsync(PlaylistTrack playlistTrack, CancellationToken cancellationToken)
        {
            await db.PlaylistTracks.AddAsync(playlistTrack, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            db.Playlists.Remove(playlist);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Playlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await db.Playlists.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Playlist?> GetByIdWithTrackAsync(Guid id, CancellationToken cancellationToken)
        {
            return await db.Playlists
                .Include(p => p.Tracks.OrderBy(t => t.Position))
                .ThenInclude(pt => pt.Track)
                .ThenInclude(t => t.Artist)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Playlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await db.Playlists
                .Where(p => p.UserId == userId)
                .Include(p => p.Tracks.OrderBy(t => t.Position))
                .ThenInclude(p => p.Track)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<PlaylistTrack?> GetPlaylistTrackAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken)
        {
            return await db.PlaylistTracks.FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId, cancellationToken);
        }

        public async Task RemoveTrackAsync(PlaylistTrack playlistTrack, CancellationToken cancellationToken)
        {
            db.PlaylistTracks.Remove(playlistTrack);
            await db.SaveChangesAsync();
        }

        public async Task UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            db.Playlists.Update(playlist);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
