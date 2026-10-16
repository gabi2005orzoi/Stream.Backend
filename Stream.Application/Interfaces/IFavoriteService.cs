using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IFavoriteService
    {
        Task<bool> AddAsync(Guid trackId, CancellationToken cancellationToken);
        Task<bool> RemoveAsync(Guid trackId, CancellationToken cancellationToken);
        Task<IReadOnlyList<Track>> GetAllAsync(CancellationToken cancellationToken);
        Task<bool> IsFavoriteAsync(Guid trackId, CancellationToken cancellationToken);
    }
}
