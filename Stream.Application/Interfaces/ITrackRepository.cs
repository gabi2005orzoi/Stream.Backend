using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface ITrackRepository
    {
        Task<Track?> FindByTitleAndArtistAsync(
            string title,
            string artist,
            CancellationToken cancellationToken);

        Task AddAsync(
            Track track,
            CancellationToken cancellationToken);

        Task<Track?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<Track> tracks, CancellationToken cancellationToken);
        Task<(IReadOnlyList<Track> Items, int TotalCount)> SearchAsync(string query, int pageNumber, int pageSize, CancellationToken cancellationToken);
    }
}
