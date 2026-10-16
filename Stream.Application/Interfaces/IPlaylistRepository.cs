using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IPlaylistRepository
    {
        Task<Playlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Playlist?> GetByIdWithTrackAsync (Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Playlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddAsync(Playlist playlist, CancellationToken cancellationToken);
        Task DeleteAsync(Playlist playlist, CancellationToken cancellationToken);
        Task AddTrackAsync(PlaylistTrack playlistTrack, CancellationToken cancellationToken);
        Task RemoveTrackAsync(PlaylistTrack playlistTrack, CancellationToken cancellationToken);
        Task<PlaylistTrack?> GetPlaylistTrackAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken);
        Task UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken);
    }
}
