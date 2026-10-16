using Stream.Application.DTOs.Common;
using Stream.Application.DTOs.ListeningHistory;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly IHistoryRepository historyRepository;
        private readonly ICurrentUserService currentUserService;

        public HistoryService(IHistoryRepository historyRepository, ICurrentUserService currentUserService)
        {
            this.historyRepository = historyRepository;
            this.currentUserService = currentUserService;
        }

        public async Task ClearHistoryAsync(CancellationToken cancellationToken)
        {
            await historyRepository.ClearByUserIdAsync(currentUserService.UserId, cancellationToken);
        }

        public async Task<IReadOnlyList<HistoryItemDto>> GetRecentHistoryAsync(int limit, CancellationToken cancellationToken)
        {
            var entities = await historyRepository.GetRecentByUserIdAsync(currentUserService.UserId, limit, cancellationToken);

            return entities.Select(h => new HistoryItemDto
            {
                Id = h.Id,
                TrackId = h.TrackId,
                Title = h.Track.Title,
                ArtistName = h.Track.Artist.Name,
                ArtworkUrl = h.Track.ArtworkUrl,
                Duration = h.Track.Duration,
                PlayedAt = h.PlayedAt,
                Completed = h.Completed
            }).ToList();
        }

        public async Task<PagedResult<HistoryItemDto>> GetPagedHistoryAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var (entities, totalCount) = await historyRepository.GetPagedByUserIdAsync(
                currentUserService.UserId, pageNumber, pageSize, cancellationToken);

            var items = entities.Select(h => new HistoryItemDto
            {
                Id = h.Id,
                TrackId = h.TrackId,
                Title = h.Track.Title,
                ArtistName = h.Track.Artist.Name,
                ArtworkUrl = h.Track.ArtworkUrl,
                Duration = h.Track.Duration,
                PlayedAt = h.PlayedAt,
                Completed = h.Completed
            }).ToList();

            return new PagedResult<HistoryItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task RecordPlayAsync(RecordHistoryDto dto, CancellationToken cancellationToken)
        {
            var history = new ListeningHistory
            {
                Id = Guid.NewGuid(),
                UserId = currentUserService.UserId,
                TrackId = dto.TrackId,
                PlayedAt = DateTime.UtcNow,
                Duration = dto.Duration ?? TimeSpan.Zero,
                Completed = dto.Completed
            };
            
            await historyRepository.AddAsync(history, cancellationToken);
        }
    }
}   