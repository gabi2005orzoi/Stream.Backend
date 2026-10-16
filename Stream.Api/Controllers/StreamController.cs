using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Stream.Application.DTOs;
using Stream.Application.Interfaces;

namespace Stream.Api.Controllers
{
    [Route("api/stream")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        private readonly IMusicProviderManager providerManager;
        private readonly ITrackRepository trackRepository;
        private readonly IMemoryCache cache;

        public StreamController(IMusicProviderManager providerManager, ITrackRepository trackRepository, IMemoryCache cache)
        {
            this.providerManager = providerManager;
            this.trackRepository = trackRepository;
            this.cache = cache;
        }

        [HttpGet("{externalId}")]
        public async Task<IActionResult> GetPlaybackSource(
            [FromRoute] string externalId,
            [FromQuery] string? provider = null,
            CancellationToken cancellationToken = default)
        {
            var source = await providerManager.GetPlaybackSourceAsync(externalId, provider, cancellationToken);

            if (source == null)
                return NotFound(new { message = "Audio stream couldn't be extracted for this track" });

            return Ok(source);
        }

        [HttpGet("track/{trackId:guid}")]
        public async Task<IActionResult> GetPlaybackSourceById(
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"playback_track_{trackId}";

            if(cache.TryGetValue(cacheKey, out PlaybackSource? cachedSource) && cachedSource != null)
                return Ok(cachedSource);

            var track = await trackRepository.GetByIdAsync(trackId, cancellationToken);

            if (track == null)
                return NotFound(new { message = "Track not found in db" });

            var providerTrack = track.ProviderTracks.FirstOrDefault();

            if (providerTrack == null)
                return NotFound(new { message = "Track with no associated provider" });

            var source = await providerManager.GetPlaybackSourceAsync(providerTrack.ExternalId, providerTrack.Provider,  cancellationToken);

            if (source == null)
                return NotFound(new { message = "Couldn't extract audio stream" });

            cache.Set(cacheKey, source, TimeSpan.FromHours  (3));

            return Ok(source);
        }

        [HttpGet("play/{externalId}")]
        public async Task<IActionResult> PlayByExternalId(
            [FromRoute] string externalId,
            [FromQuery] string? provider = null,
            CancellationToken cancellationToken = default)
        {
            var audio = await providerManager.GetAudioStreamAsync(externalId, provider ,cancellationToken);

            if(audio == null)
                return NotFound(new {message = "Couldn't stream audio for this track"});

               return File(audio.Stream, audio.MimeType, enableRangeProcessing: false);
        }

        [HttpGet("track/{trackId:guid}/play")]
        public async Task<IActionResult> PlayByTrackId(
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var track = await trackRepository.GetByIdAsync(trackId, cancellationToken);

            if (track == null)
                return NotFound(new { message = "Track not found" });

            foreach(var pt in track.ProviderTracks)
            {
                var audio = await providerManager.GetAudioStreamAsync(pt.ExternalId, pt.Provider, cancellationToken);
                if (audio != null)
                    return File(audio.Stream, audio.MimeType, enableRangeProcessing: true);
            }

            return NotFound(new { message = "Audio stream unavailable across all providers" });
        }
    }
}
