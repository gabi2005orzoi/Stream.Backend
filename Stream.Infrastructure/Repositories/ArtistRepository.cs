using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Infrastructure.Repositories
{
    public class ArtistRepository: IArtistRepository
    {
        private readonly AppDbContext db;

        public ArtistRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Artist?> FindByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await db.Artists.FirstOrDefaultAsync(a => a.Name == name, cancellationToken);
        }

        public async Task AddAsync(Artist artist, CancellationToken cancellationToken)
        {
            await db.Artists.AddAsync(artist, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Artist>> FindByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken)
        {
            var namesList = names.ToList();
            return await db.Artists
                .Where(a => namesList.Contains(a.Name))
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken)
        {
            await db.Artists.AddRangeAsync(artists, cancellationToken);
        }
    }
}
