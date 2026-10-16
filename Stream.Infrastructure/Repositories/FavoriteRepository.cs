using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Infrastructure.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly AppDbContext db;

        public FavoriteRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
        {
            return await db.Favorites.AnyAsync(f => f.UserId ==  userId && f.TrackId == trackId, cancellationToken);
        }

        public async Task<Favorite?> FindAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
        {
            return await db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.TrackId == trackId, cancellationToken);
        }

        public async Task AddAsync(Favorite favorite, CancellationToken cancellationToken)
        {
            await db.Favorites.AddAsync(favorite, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(Favorite favorite, CancellationToken cancellationToken)
        {
            db.Favorites.Remove(favorite);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Track>> GetFavoritesTracksByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await db.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Include(f => f.Track).ThenInclude(t => t.Artist)
                .Include(f => f.Track).ThenInclude(t => t.ProviderTracks)
                .Select(f => f.Track)
                .ToListAsync(cancellationToken);
        }
    }
}
