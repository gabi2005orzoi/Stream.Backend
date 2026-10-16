using Stream.Application.DTOs;
using Stream.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface ISearchService
    {
        Task<IReadOnlyList<SearchResult>> SearchAsync(
            string query,
            CancellationToken cancellationToken);

        Task<PagedResult<SearchResult>> SearchPagedAsync(
            string query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);
    }
}
