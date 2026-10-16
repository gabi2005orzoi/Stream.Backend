using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IHistoryRepository
    {
        Task AddAsync(ListeningHistory history, CancellationToken cancellationToken);
        Task<IReadOnlyList<ListeningHistory>> GetRecentByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken);
        Task<(IReadOnlyList<ListeningHistory> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<IReadOnlyList<Track>> GetTopTracksAsync(int limit, CancellationToken cancellationToken);
    }
}
