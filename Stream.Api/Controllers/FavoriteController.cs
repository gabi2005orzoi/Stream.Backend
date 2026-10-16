using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Stream.Domain.Models;

namespace Stream.Api.Controllers
{
    [Authorize]
    [Route("api/favorites")]
    [ApiController]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService favoriteService;

        public FavoriteController(IFavoriteService favoriteService)
        {
            this.favoriteService = favoriteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var tracks = await favoriteService.GetAllAsync(cancellationToken);

            var result = tracks.Select(t => new TrackDto
            {
                Id = t.Id,
                Title = t.Title,
                ArtistName = t.Artist.Name,
                Duration = t.Duration,
                ArtworkUrl = t.ArtworkUrl,
                Providers = t.ProviderTracks.Select(pt => new ProviderTrackDto
                {
                    Provider = pt.Provider,
                    ExternalId = pt.ExternalId,
                    ExternalUrl = pt.ExternalUrl
                }).ToList()
            });

            return Ok(result);
        }

        [HttpPost("{trackId:guid}")]
        public async Task<IActionResult> Add(
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var added = await favoriteService.AddAsync(trackId, cancellationToken);
            if (!added)
                return BadRequest(new { message = "Track already into favorites" });

            return Ok(new { message = "Song added successfully into favorites" });
        }

        [HttpDelete("{trackId:guid}")]
        public async Task<IActionResult> Remove(
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var removed = await favoriteService.RemoveAsync(trackId, cancellationToken);
            if (!removed)
                return BadRequest(new { message = "Track not in favorites" });

            return Ok(new { message = "Song removed successfully from favorites" });
        }

        [HttpGet("{trackId:guid}/check")]
        public async Task<IActionResult> IsFavorite(
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var isFav = await favoriteService.IsFavoriteAsync(trackId, cancellationToken);
            return Ok(new { isFavorite = isFav });
        }
    }
}
