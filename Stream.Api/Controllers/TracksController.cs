using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stream.Application.DTOs;
using Stream.Application.Interfaces;

namespace Stream.Api.Controllers
{
    [Route("api/tracks")]
    [ApiController]
    public class TracksController : ControllerBase
    {
        private readonly ITrackResolver trackResolver;
        private readonly ITrackRepository trackRepository;
        private readonly IMusicProviderManager providerManager;
        private readonly IRecommendationService recommendationService;

        public TracksController(
            ITrackResolver trackResolver,
            ITrackRepository trackRepository,
            IMusicProviderManager providerManager,
            IRecommendationService recommendationService)
        {
            this.trackResolver = trackResolver;
            this.trackRepository = trackRepository;
            this.providerManager = providerManager;
            this.recommendationService = recommendationService;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var track = await trackRepository.GetByIdAsync(id, cancellationToken);

            if(track == null)
                return NotFound(new {message = "Track not found"});

            return Ok(new TrackDto
            {
                Id = track.Id,
                Title = track.Title,
                ArtistName = track.Artist.Name,
                Duration = track.Duration,
                ArtworkUrl =  track.ArtworkUrl,
                Providers = track.ProviderTracks.Select(pt => new ProviderTrackDto
                {
                    Provider = pt.Provider,
                    ExternalId = pt.ExternalId,
                    ExternalUrl = pt.ExternalUrl
                }).ToList()
            });
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> Resolve(
            [FromBody] TrackCandidate candidate,
            CancellationToken cancellationToken)
        {
            var track = await trackResolver.ResolveAsync(candidate, cancellationToken);

            return Ok(new TrackDto
            {
                Id = track.Id,
                Title = track.Title,
                ArtistName = track.Artist.Name,
                Duration = track.Duration,
                ArtworkUrl = track.ArtworkUrl,
                Providers = track.ProviderTracks.Select(pt => new ProviderTrackDto
                {
                    Provider = pt.Provider,
                    ExternalId = pt.ExternalId,
                    ExternalUrl = pt.ExternalUrl
                }).ToList()
            });
        }

        [HttpGet("{id:guid}/radio")]
        public async Task<IActionResult> GetRadioTracks(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var track = await trackRepository.GetByIdAsync(id, cancellationToken);
            if (track == null)
                return NotFound(new { message = "Track not found" });

            var radioTracks = await recommendationService.GetRadioTracksAsync(id, 8, cancellationToken);
            return Ok(radioTracks);
        }
    }
}
