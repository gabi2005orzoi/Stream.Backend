using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;

namespace Stream.Infrastructure.Repositories
{
    public class TrackRepository: ITrackRepository
    {
        private readonly AppDbContext db;

        public TrackRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Track?> FindByTitleAndArtistAsync(string title, string artist, CancellationToken cancellationToken)
        {
            return await db.Tracks
                .Include(t => t.Artist)
                .Include(t => t.ProviderTracks)
                .FirstOrDefaultAsync(t => t.Title == title && t.Artist.Name == artist, cancellationToken);
        }

        public async Task AddAsync(Track track, CancellationToken cancellationToken)
        {
            await db.Tracks.AddAsync(track, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Track?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await db.Tracks
                .Include(t => t.Artist)
                .Include(t => t.ProviderTracks)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<Track> tracks, CancellationToken cancellationToken)
        {
            await db.Tracks.AddRangeAsync(tracks, cancellationToken);
        }

        public async Task<(IReadOnlyList<Track> Items, int TotalCount)> SearchAsync(string query, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var q = query.Trim().ToLower();

            var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2)
                .ToList();

            var dbQuery = db.Tracks
                .Include(t => t.Artist)
                .Include(t => t.ProviderTracks)
                .AsQueryable();

            if (words.Count == 0)
            {
                dbQuery = dbQuery.Where(t => t.Title.ToLower().Contains(q) || t.Artist.Name.ToLower().Contains(q));
            }
            else
            {
                foreach (var w in words)
                {
                    var word = w;
                    dbQuery = dbQuery.Where(t => t.Title.ToLower().Contains(word) || t.Artist.Name.ToLower().Contains(word));
                }
            }

            var totalCount = await dbQuery.CountAsync(cancellationToken);

            var items = await dbQuery
                .OrderBy(t => t.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
