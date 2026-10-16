using Microsoft.Extensions.Caching.Memory;
using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class MusicProviderManager : IMusicProviderManager
    {
        private readonly IEnumerable<IMusicProvider> providers;
        private readonly IMemoryCache cache;

        public MusicProviderManager(IEnumerable<IMusicProvider> providers, IMemoryCache cache)
        {
            this.providers = providers;
            this.cache = cache;
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
                catch
                {

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
                    var audioSteam = await provider.GetAudioStreamAsync(externalId, cancellationToken);
                    if (audioSteam != null)
                        return audioSteam;
                }
                catch
                {

                }
            }
            return null;
        }
    }
}
