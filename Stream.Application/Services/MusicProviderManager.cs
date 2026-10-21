using Microsoft.Extensions.Caching.Memory;
using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stream.Application.Services
{
    public class MusicProviderManager : IMusicProviderManager
    {
        private readonly IEnumerable<IMusicProvider> providers;
        private readonly IMemoryCache cache;
        private readonly IProviderTrackRepository providerTrackRepository;

        public MusicProviderManager(
            IEnumerable<IMusicProvider> providers, 
            IMemoryCache cache,
            IProviderTrackRepository providerTrackRepository)
        {
            this.providers = providers;
            this.cache = cache;
            this.providerTrackRepository = providerTrackRepository;
        }

        public async Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var cacheKey = $"provider_search_{query.Trim().ToLowerInvariant()}";
            if (cache.TryGetValue(cacheKey, out IReadOnlyList<TrackCandidate>? cached) && cached != null)
                return cached;

            var results = new List<TrackCandidate>();
            foreach(var provider in providers)
            {
                try
                {
                    var providerResult = await provider.SearchAsync(query, cancellationToken);
                    results.AddRange(providerResult);
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error searching with provider {provider.Name}: {ex.Message}");
                }
            }

            if (results.Count > 0)
            {
                cache.Set(cacheKey, (IReadOnlyList<TrackCandidate>)results, TimeSpan.FromHours(1));
            }

            return results;
        }

        public async Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, string? providerName = null, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"playback_{externalId}";

            if (cache.TryGetValue(cacheKey, out PlaybackSource? cachedSource) && cachedSource != null)
                return cachedSource;

            var targetProviders = string.IsNullOrWhiteSpace(providerName)
                ? providers
                : providers.Where(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            foreach(var provider in targetProviders)
            {
                try
                {
                    var source = await provider.GetPlaybackSourceAsync(externalId, cancellationToken);

                    if (source != null)
                    {
                        cache.Set(cacheKey, source, TimeSpan.FromHours(3));
                        return source;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MusicProviderManager] Provider {provider.Name} playback error for {externalId}: {ex.Message}");
                }
            }

            return null;
        }

        public async Task<AudioStreamResult?> GetAudioStreamAsync(string externalId, string? providerName = null, CancellationToken cancellationToken = default)
        {
            var targetProviders = string.IsNullOrWhiteSpace(providerName)
                ? providers
                : providers.Where(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            foreach(var provider in targetProviders)
            {
                try
                {
                    var audioStream = await provider.GetAudioStreamAsync(externalId, cancellationToken);
                    if (audioStream != null)
                        return audioStream;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MusicProviderManager] Provider {provider.Name} audio stream error for {externalId}: {ex.Message}");
                }
            }
            return null;
        }

        public async Task<PlaybackSource?> GetPlaybackSourceForTrackAsync(Track track, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"playback_track_{track.Id}";
            if (cache.TryGetValue(cacheKey, out PlaybackSource? cachedSource) && cachedSource != null)
                return cachedSource;

            var triedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Try existing ProviderTracks first
            foreach (var pt in track.ProviderTracks)
            {
                triedProviders.Add(pt.Provider);
                try
                {
                    var source = await GetPlaybackSourceAsync(pt.ExternalId, pt.Provider, cancellationToken);
                    if (source != null)
                    {
                        cache.Set(cacheKey, source, TimeSpan.FromHours(3));
                        return source;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MusicProviderManager] Track {track.Id} provider {pt.Provider} failed: {ex.Message}");
                }
            }

            // 2. Fallback: If existing providers failed (e.g. YouTube blocked on Render), search alternative providers
            var fallbackProviders = providers.Where(p => !triedProviders.Contains(p.Name)).ToList();
            if (fallbackProviders.Count == 0)
                return null;

            var artistName = track.Artist?.Name ?? string.Empty;
            var queries = BuildFallbackQueries(track.Title, artistName);

            foreach (var provider in fallbackProviders)
            {
                foreach (var query in queries)
                {
                    try
                    {
                        var candidates = await provider.SearchAsync(query, cancellationToken);
                        if (candidates == null || candidates.Count == 0)
                            continue;

                        // Try each candidate until one streams successfully
                        foreach (var candidate in candidates.Take(3))
                        {
                            var source = await provider.GetPlaybackSourceAsync(candidate.ExternalId, cancellationToken);
                            if (source != null)
                            {
                                await LinkNewProviderTrackAsync(track, candidate, cancellationToken);
                                cache.Set(cacheKey, source, TimeSpan.FromHours(3));
                                return source;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MusicProviderManager] Fallback provider {provider.Name} failed for '{query}': {ex.Message}");
                    }
                }
            }

            return null;
        }

        public async Task<AudioStreamResult?> GetAudioStreamForTrackAsync(Track track, CancellationToken cancellationToken = default)
        {
            var triedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Try existing ProviderTracks
            foreach (var pt in track.ProviderTracks)
            {
                triedProviders.Add(pt.Provider);
                try
                {
                    var audio = await GetAudioStreamAsync(pt.ExternalId, pt.Provider, cancellationToken);
                    if (audio != null)
                        return audio;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MusicProviderManager] Track {track.Id} audio stream provider {pt.Provider} failed: {ex.Message}");
                }
            }

            // 2. Fallback: Query alternative providers
            var fallbackProviders = providers.Where(p => !triedProviders.Contains(p.Name)).ToList();
            if (fallbackProviders.Count == 0)
                return null;

            var artistName = track.Artist?.Name ?? string.Empty;
            var queries = BuildFallbackQueries(track.Title, artistName);

            foreach (var provider in fallbackProviders)
            {
                foreach (var query in queries)
                {
                    try
                    {
                        var candidates = await provider.SearchAsync(query, cancellationToken);
                        if (candidates == null || candidates.Count == 0)
                            continue;

                        foreach (var candidate in candidates.Take(3))
                        {
                            var audio = await provider.GetAudioStreamAsync(candidate.ExternalId, cancellationToken);
                            if (audio != null)
                            {
                                await LinkNewProviderTrackAsync(track, candidate, cancellationToken);
                                return audio;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MusicProviderManager] Fallback audio stream provider {provider.Name} failed for '{query}': {ex.Message}");
                    }
                }
            }

            return null;
        }

        private static List<string> BuildFallbackQueries(string title, string artist)
        {
            var queries = new List<string>();
            var cleanTitle = title?.Trim() ?? string.Empty;
            var cleanArtist = artist?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(cleanArtist) && !string.IsNullOrWhiteSpace(cleanTitle))
            {
                if (cleanTitle.Contains(cleanArtist, StringComparison.OrdinalIgnoreCase))
                {
                    queries.Add(cleanTitle);
                }
                else
                {
                    queries.Add($"{cleanArtist} {cleanTitle}");
                    queries.Add(cleanTitle);
                }
            }
            else if (!string.IsNullOrWhiteSpace(cleanTitle))
            {
                queries.Add(cleanTitle);
            }

            return queries.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task LinkNewProviderTrackAsync(Track track, TrackCandidate candidate, CancellationToken cancellationToken)
        {
            try
            {
                var newPt = new ProviderTrack
                {
                    Id = Guid.NewGuid(),
                    TrackId = track.Id,
                    Track = track,
                    Provider = candidate.Provider,
                    ExternalId = candidate.ExternalId,
                    ExternalUrl = candidate.ExternalUrl,
                    IsPlayable = true,
                    LastCheckedAt = DateTime.UtcNow
                };

                await providerTrackRepository.AddAsync(newPt, cancellationToken);
                track.ProviderTracks.Add(newPt);
                Console.WriteLine($"[MusicProviderManager] Successfully linked fallback provider '{candidate.Provider}' ({candidate.ExternalId}) for track '{track.Title}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicProviderManager] Notice: Could not save fallback ProviderTrack: {ex.Message}");
            }
        }
    }
}
