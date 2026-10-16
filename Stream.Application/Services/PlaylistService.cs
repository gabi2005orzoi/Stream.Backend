using Stream.Application.DTOs.PlaylistDTOs;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class PlaylistService: IPlaylistService
    {
        IPlaylistRepository playlistRepository;
        ICurrentUserService currentUserService;

        public PlaylistService(IPlaylistRepository playlistRepository, ICurrentUserService currentUserService)
        {
            this.playlistRepository = playlistRepository;
            this.currentUserService = currentUserService;
        }

        public async Task<bool> AddTrackAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken)
        {
            var playlist = await playlistRepository.GetByIdWithTrackAsync(playlistId, cancellationToken);

            if (playlist == null)
                return false;

            var alreadyPlaylist = playlist.Tracks.Any(pt => pt.TrackId == trackId);
            if (alreadyPlaylist)
                return false;

            var playlistTrack = new PlaylistTrack
            {
                PlaylistId = playlistId,
                TrackId = trackId,
                Position = playlist.Tracks.Count + 1,
                AddedAt = DateTime.UtcNow,
            };

            await playlistRepository.AddTrackAsync(playlistTrack, cancellationToken);
            return true;
        }

        public async Task<PlaylistSummaryDto> CreateAsync(CreatePlaylistDto dto, CancellationToken cancellationToken)
        {
            var playlist = new Playlist
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                UserId = currentUserService.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await playlistRepository.AddAsync(playlist, cancellationToken);

            return new PlaylistSummaryDto
            {
                Id = playlist.Id,
                Name = playlist.Name,
                Description = playlist.Description,
                TracksCount = 0,
                CreatedAt = playlist.CreatedAt,
            };
        }

        public async Task<bool> DeleteAsync(Guid playlistId, CancellationToken cancellationToken)
        {
            var playlist = await playlistRepository.GetByIdAsync(playlistId, cancellationToken);

            if (playlist == null || playlist.UserId != currentUserService.UserId)
                return false;

            await playlistRepository.DeleteAsync(playlist, cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<PlaylistSummaryDto>> GetAllFromCurrentUserAsync(CancellationToken cancellationToken)
        {
            var playlists = await playlistRepository.GetByUserIdAsync(currentUserService.UserId, cancellationToken);

            return playlists.Select(p => new PlaylistSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                TracksCount = p.Tracks.Count,
                CreatedAt = p.CreatedAt,
                ArtworkUrls = p.Tracks
                    .OrderBy(t => t.Position)
                    .Where(t => !string.IsNullOrEmpty(t.Track.ArtworkUrl))
                    .Select(t => t.Track.ArtworkUrl!)
                    .Take(4)
                    .ToList()
            }).ToList();
        }

        public async Task<PlaylistDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var playlist = await playlistRepository.GetByIdWithTrackAsync(id, cancellationToken);

            if (playlist == null)
                return null;

            return new PlaylistDetailsDto
            {
                Id = playlist.Id,
                Name = playlist.Name,
                Description = playlist.Description,
                CreatedAt = playlist.CreatedAt,
                IsOwner = playlist.UserId == currentUserService.UserId,
                Tracks = playlist.Tracks.Select(pt => new PlaylistTrackDto
                {
                    TrackId = pt.TrackId,
                    Title = pt.Track.Title,
                    ArtistName = pt.Track.Artist.Name,
                    Duration = pt.Track.Duration,
                    ArtworkUrl = pt.Track.ArtworkUrl,
                    Position = pt.Position,
                    AddedAt = pt.AddedAt
                }).ToList(),
            };

        }

        public async Task<bool> RemoveTrackAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken)
        {
            var playlist = await playlistRepository.GetByIdAsync(playlistId, cancellationToken);

            if (playlist == null)
                return false;

            var track = await playlistRepository.GetPlaylistTrackAsync(playlistId, trackId, cancellationToken);

            if (track == null)
                return false;

            await playlistRepository.RemoveTrackAsync(track, cancellationToken);
            return true;
        }

        public async Task<bool> UpdateAsync(Guid playlistId, UpdatePlaylistkDto dto, CancellationToken cancellationToken)
        {
            var playlist = await playlistRepository.GetByIdAsync(playlistId, cancellationToken);

            if (playlist == null || playlist.UserId != currentUserService.UserId)
                return false;

            playlist.Name = dto.Name;
            playlist.Description = dto.Description;

            await playlistRepository.UpdatePlaylistAsync(playlist, cancellationToken);
            return true;
        }
    }
}
