using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IFavoriteRepository
    {
        Task<bool> ExistsAsync(Guid userId, Guid trackId, CancellationToken cancellationToken);
        Task<Favorite?> FindAsync(Guid userId, Guid trackId, CancellationToken cancellationToken);
        Task AddAsync(Favorite favorite, CancellationToken cancellationToken);
        Task RemoveAsync(Favorite favorite, CancellationToken cancellationToken);
        Task<IReadOnlyList<Track>> GetFavoritesTracksByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    }
}
