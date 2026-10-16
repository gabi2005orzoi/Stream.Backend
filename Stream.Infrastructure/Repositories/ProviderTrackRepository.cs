using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Infrastructure.Repositories
{
    public class ProviderTrackRepository: IProviderTrackRepository
    {
        private AppDbContext db;

        public ProviderTrackRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<ProviderTrack?> FindAsync(Guid trackId, string provider, string externalId, CancellationToken cancellationToken)
        {
            return await db.ProviderTracks.FirstOrDefaultAsync(pt =>
                pt.TrackId == trackId &&
                pt.Provider == provider &&
                pt.ExternalId == externalId,
            cancellationToken);
        }

        public async Task AddAsync(ProviderTrack track, CancellationToken cancellationToken)
        {
            await db.ProviderTracks.AddAsync(track, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProviderTrack?> FindByProviderAndExternalIdAsync(string provider, string externalId, CancellationToken cancellationToken)
        {
            return await db.ProviderTracks
                .Include(pt => pt.Track)
                    .ThenInclude(t => t.Artist)
                .FirstOrDefaultAsync(pt =>
                    pt.Provider == provider &&
                    pt.ExternalId == externalId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderTrack>> FindByExternalIdsAsync(string provider, IEnumerable<string> externalIds, CancellationToken cancellationToken)
        {
            var idsList = externalIds.ToList();
            return await db.ProviderTracks
                .Include(pt => pt.Track)
                    .ThenInclude(t => t.Artist)
                .Include(pt => pt.Track)
                    .ThenInclude(t => t.ProviderTracks)
                .Where(pt => pt.Provider == provider && idsList.Contains(pt.ExternalId))
                .ToListAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<ProviderTrack> tracks, CancellationToken cancellationToken)
        {
            await db.ProviderTracks.AddRangeAsync(tracks, cancellationToken);
        }
    }
}
