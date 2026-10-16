using Stream.Application.DTOs.PlaylistDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IPlaylistService
    {
        Task<PlaylistSummaryDto> CreateAsync(CreatePlaylistDto dto, CancellationToken cancellationToken);
        Task<IReadOnlyList<PlaylistSummaryDto>> GetAllFromCurrentUserAsync(CancellationToken cancellationToken);
        Task<PlaylistDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> AddTrackAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken);
        Task<bool> RemoveTrackAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid playlistId, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(Guid playlistId, UpdatePlaylistkDto dto, CancellationToken cancellationToken);
    }
}
