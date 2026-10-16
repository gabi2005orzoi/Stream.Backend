using Stream.Application.DTOs.Common;
using Stream.Application.DTOs.ListeningHistory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IHistoryService
    {
        Task RecordPlayAsync(RecordHistoryDto dto, CancellationToken cancellationToken);
        Task<IReadOnlyList<HistoryItemDto>> GetRecentHistoryAsync(int limit, CancellationToken cancellationToken);
        Task<PagedResult<HistoryItemDto>> GetPagedHistoryAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task ClearHistoryAsync(CancellationToken cancellationToken);
    }
}
