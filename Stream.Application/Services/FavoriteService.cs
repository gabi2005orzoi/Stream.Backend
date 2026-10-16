using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    internal class FavoriteService : IFavoriteService
    {

        private readonly ICurrentUserService currentUserService;
        private readonly IFavoriteRepository favoriteRepository;

        public FavoriteService(ICurrentUserService currentUserService, IFavoriteRepository favoriteRepository)
        {
            this.currentUserService = currentUserService;
            this.favoriteRepository = favoriteRepository;
        }

        public async Task<bool> AddAsync(Guid trackId, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;

            var alreadyExists = await favoriteRepository.ExistsAsync(userId, trackId, cancellationToken);

            if (alreadyExists)
                return false;

            var favorite = new Favorite
            {
                UserId = userId,
                TrackId = trackId,
                CreatedAt = DateTime.UtcNow
            };

            await favoriteRepository.AddAsync(favorite, cancellationToken);
            return true;
        }

        public async Task<bool> RemoveAsync(Guid trackId, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;

            var favorite = await favoriteRepository.FindAsync(userId, trackId, cancellationToken);

            if (favorite == null)
                return false;

            await favoriteRepository.RemoveAsync(favorite, cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<Track>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await favoriteRepository.GetFavoritesTracksByUserIdAsync(currentUserService.UserId, cancellationToken);
        }

        public async Task<bool> IsFavoriteAsync(Guid trackId, CancellationToken cancellationToken)
        {
            return await favoriteRepository.ExistsAsync(currentUserService.UserId, trackId, cancellationToken);
        }
    }
}
