using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IArtistRepository
    {
        Task<Artist?> FindByNameAsync(string name, CancellationToken cancellationToken);
        Task AddAsync(Artist artist, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Artist>> FindByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken);
    }
}
