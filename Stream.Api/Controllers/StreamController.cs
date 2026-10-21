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
        private readonly IProviderTrackRepository providerTrackRepository;
        private readonly IMemoryCache cache;

        public StreamController(
            IMusicProviderManager providerManager, 
            ITrackRepository trackRepository, 
            IProviderTrackRepository providerTrackRepository,
            IMemoryCache cache)
        {
            this.providerManager = providerManager;
            this.trackRepository = trackRepository;
            this.providerTrackRepository = providerTrackRepository;
            this.cache = cache;
        }

        [HttpGet("{externalId}")]
        public async Task<IActionResult> GetPlaybackSource(
            [FromRoute] string externalId,
            [FromQuery] string? provider = null,
            CancellationToken cancellationToken = default)
        {
            var source = await providerManager.GetPlaybackSourceAsync(externalId, provider, cancellationToken);

            if (source != null)
                return Ok(source);

            // Fallback: If provider failed (e.g. YouTube blocked on Render), find track in DB and fallback to other providers
            var pt = await providerTrackRepository.FindByProviderAndExternalIdAsync(provider ?? "Youtube", externalId, cancellationToken);
            if (pt?.Track != null)
            {
                var fallbackSource = await providerManager.GetPlaybackSourceForTrackAsync(pt.Track, cancellationToken);
                if (fallbackSource != null)
                    return Ok(fallbackSource);
            }

            return NotFound(new { message = "Audio stream couldn't be extracted for this track across all providers" });
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

            var source = await providerManager.GetPlaybackSourceForTrackAsync(track, cancellationToken);

            if (source == null)
                return NotFound(new { message = "Couldn't extract audio stream across all providers" });

            cache.Set(cacheKey, source, TimeSpan.FromHours(3));

            return Ok(source);
        }

        [HttpGet("play/{externalId}")]
        public async Task<IActionResult> PlayByExternalId(
            [FromRoute] string externalId,
            [FromQuery] string? provider = null,
            CancellationToken cancellationToken = default)
        {
            var audio = await providerManager.GetAudioStreamAsync(externalId, provider, cancellationToken);

            if (audio != null)
                return File(audio.Stream, audio.MimeType, enableRangeProcessing: false);

            // Fallback: If direct stream failed, try other providers for this track
            var pt = await providerTrackRepository.FindByProviderAndExternalIdAsync(provider ?? "Youtube", externalId, cancellationToken);
            if (pt?.Track != null)
            {
                var fallbackAudio = await providerManager.GetAudioStreamForTrackAsync(pt.Track, cancellationToken);
                if (fallbackAudio != null)
                    return File(fallbackAudio.Stream, fallbackAudio.MimeType, enableRangeProcessing: true);
            }

            return NotFound(new { message = "Couldn't stream audio for this track across all providers" });
        }

        [HttpGet("track/{trackId:guid}/play")]
        public async Task<IActionResult> PlayByTrackId(
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var track = await trackRepository.GetByIdAsync(trackId, cancellationToken);

            if (track == null)
                return NotFound(new { message = "Track not found" });

            var audio = await providerManager.GetAudioStreamForTrackAsync(track, cancellationToken);

            if (audio != null)
                return File(audio.Stream, audio.MimeType, enableRangeProcessing: true);

            return NotFound(new { message = "Audio stream unavailable across all providers" });
        }
    }
}
