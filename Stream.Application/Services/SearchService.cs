using Microsoft.Extensions.Caching.Memory;
using Stream.Application.DTOs;
using Stream.Application.DTOs.Common;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IMusicProviderManager providerManager;
        private readonly IMemoryCache cache;
        private readonly ITrackResolver trackResolver;
        private readonly ITrackRepository trackRepository;

        public SearchService(
            IMusicProviderManager providerManager, 
            IMemoryCache cache, 
            ITrackResolver trackResolver,
            ITrackRepository trackRepository)
        {
            this.providerManager = providerManager;
            this.cache = cache;
            this.trackResolver = trackResolver;
            this.trackRepository = trackRepository;
        }

        private static string NormalizeArtwork(string? artworkUrl, string externalId, string provider)
        {
            if (string.IsNullOrWhiteSpace(artworkUrl) 
                || artworkUrl.Contains("invidious", StringComparison.OrdinalIgnoreCase) 
                || artworkUrl.StartsWith("/vi/", StringComparison.OrdinalIgnoreCase))
            {
                if (provider.Equals("Invidious", StringComparison.OrdinalIgnoreCase) 
                    || provider.Equals("Youtube", StringComparison.OrdinalIgnoreCase) 
                    || externalId.Length == 11)
                {
                    return $"https://i.ytimg.com/vi/{externalId}/hqdefault.jpg";
                }
            }
            return artworkUrl ?? string.Empty;
        }

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var cacheKey = $"search_{query.Trim().ToLowerInvariant()}";

            if (cache.TryGetValue(cacheKey, out IReadOnlyList<SearchResult>? cachedResult) && cachedResult != null)
                return cachedResult;

            var candidates = await providerManager.SearchAsync(query, cancellationToken);

            IReadOnlyList<Track> tracks = [];
            try
            {
                tracks = await trackResolver.ResolveBatchAsync(candidates, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SearchService] Error resolving candidates batch: {ex.Message}");
            }

            var results = new List<SearchResult>();
            var resolvedMap = new Dictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in tracks)
            {
                foreach (var pt in track.ProviderTracks)
                {
                    resolvedMap[$"{pt.Provider}_{pt.ExternalId}"] = track;
                }
            }

            foreach (var candidate in candidates)
            {
                var key = $"{candidate.Provider}_{candidate.ExternalId}";
                resolvedMap.TryGetValue(key, out var track);

                var artwork = NormalizeArtwork(track?.ArtworkUrl ?? candidate.ArtworkUrl, candidate.ExternalId, candidate.Provider);

                results.Add(new SearchResult
                {
                    TrackId = track?.Id,
                    Provider = candidate.Provider,
                    ExternalId = candidate.ExternalId,
                    Title = track?.Title ?? candidate.Title,
                    Artist = track?.Artist?.Name ?? candidate.Artist,
                    Album = candidate.Album,
                    Duration = track?.Duration ?? candidate.Duration,
                    ArtworkUrl = artwork
                });
            }

            if (results.Count > 0)
                cache.Set(cacheKey, (IReadOnlyList<SearchResult>)results, TimeSpan.FromHours(15));

            return results;
        }

        public async Task<PagedResult<SearchResult>> SearchPagedAsync(
            string query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new PagedResult<SearchResult>();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var normalizedQuery = query.Trim().ToLowerInvariant();
            var cacheKey = $"search_paged_results_{normalizedQuery}";

            if (!cache.TryGetValue(cacheKey, out IReadOnlyList<SearchResult>? allResults) || allResults == null)
            {
                var candidates = await providerManager.SearchAsync(query, cancellationToken);

                IReadOnlyList<Track> tracks = [];
                try
                {
                    tracks = await trackResolver.ResolveBatchAsync(candidates, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SearchService] Error resolving candidates batch: {ex.Message}");
                }

                var results = new List<SearchResult>();
                var resolvedMap = new Dictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
                foreach (var track in tracks)
                {
                    foreach (var pt in track.ProviderTracks)
                    {
                        resolvedMap[$"{pt.Provider}_{pt.ExternalId}"] = track;
                    }
                }

                foreach (var candidate in candidates)
                {
                    var key = $"{candidate.Provider}_{candidate.ExternalId}";
                    resolvedMap.TryGetValue(key, out var track);

                    var artwork = NormalizeArtwork(track?.ArtworkUrl ?? candidate.ArtworkUrl, candidate.ExternalId, candidate.Provider);

                    results.Add(new SearchResult
                    {
                        TrackId = track?.Id,
                        Provider = candidate.Provider,
                        ExternalId = candidate.ExternalId,
                        Title = track?.Title ?? candidate.Title,
                        Artist = track?.Artist?.Name ?? candidate.Artist,
                        Album = candidate.Album,
                        Duration = track?.Duration ?? candidate.Duration,
                        ArtworkUrl = artwork
                    });
                }

                try
                {
                    var (dbTracks, _) = await trackRepository.SearchAsync(query, 1, 10, cancellationToken);
                    var existingTrackIds = new HashSet<Guid>(results.Where(r => r.TrackId.HasValue).Select(r => r.TrackId!.Value));
                    foreach (var dt in dbTracks)
                    {
                        if (!existingTrackIds.Contains(dt.Id))
                        {
                            var pt = dt.ProviderTracks.FirstOrDefault();
                            var extId = pt?.ExternalId ?? string.Empty;
                            var prov = pt?.Provider ?? "Youtube";
                            results.Add(new SearchResult
                            {
                                TrackId = dt.Id,
                                Provider = prov,
                                ExternalId = extId,
                                Title = dt.Title,
                                Artist = dt.Artist?.Name ?? string.Empty,
                                Duration = dt.Duration,
                                ArtworkUrl = NormalizeArtwork(dt.ArtworkUrl, extId, prov)
                            });
                        }
                    }
                }
                catch
                {
                }

                allResults = results;
                if (allResults.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    cache.Set(cacheKey, allResults, TimeSpan.FromMinutes(30));
                }
            }

            var pagedItems = allResults
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<SearchResult>
            {
                Items = pagedItems,
                TotalCount = allResults.Count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
