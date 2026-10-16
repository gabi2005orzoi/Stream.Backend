using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Infrastructure.Repositories
{
    public class HistoryRepository: IHistoryRepository
    {
        private readonly AppDbContext db;

        public HistoryRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task AddAsync(ListeningHistory history, CancellationToken cancellationToken)
        {
            await db.AddAsync(history, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var entries = await db.ListeningHistory
                .Where(h => h.UserId == userId)
                .ToListAsync();

            db.ListeningHistory.RemoveRange(entries);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ListeningHistory>> GetRecentByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken)
        {
            return await db.ListeningHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.PlayedAt)
                .Take(limit)
                .Include(h => h.Track)
                .ThenInclude(t => t.Artist)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<ListeningHistory> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var query = db.ListeningHistory
                .Where(h => h.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(h => h.PlayedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(h => h.Track)
                .ThenInclude(t => t.Artist)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IReadOnlyList<Track>> GetTopTracksAsync(int limit, CancellationToken cancellationToken)
        {
            var popularTrackIds = await db.ListeningHistory
                .GroupBy(h => h.TrackId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(limit)
                .ToListAsync(cancellationToken);

            if (popularTrackIds.Count == 0)
                return [];

            return await db.Tracks
                .Where(t => popularTrackIds.Contains(t.Id))
                .Include(t => t.Artist)
                .Include(t => t.ProviderTracks)
                .ToListAsync(cancellationToken);
        }
    }
}
