using Microsoft.Extensions.Caching.Memory;
using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Stream.Application.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ICurrentUserService currentUserService;
        private readonly IHistoryRepository historyRepository;
        private readonly IFavoriteRepository favoriteRepository;
        private readonly ITrackRepository trackRepository;
        private readonly IMusicProviderManager providerManager;
        private readonly ITrackResolver trackResolver;
        private readonly IMemoryCache cache;

        private static readonly HttpClient DeezerClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly Regex BracketsRx = new(@"[\(\[\{].*?[\)\]\}]", RegexOptions.Compiled);
        private static readonly Regex FeatRx = new(@"\b(feat|ft|featuring)\b.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DashSuffixRx = new(
            @"\s[-–—]\s*(remaster(ed)?|live|remix|radio edit|single version|acoustic|mono|stereo|\d{4}\b).*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NoiseWordsRx = new(
            @"\b(official (music )?(video|audio)|music video|lyric video|lyrics?|visualizer|hd|hq|4k)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VariationRx = new(
            @"\b(live|cover|remix|karaoke|instrumental|slowed|sped up|speed up|nightcore|8d|reverb|acoustic|mashup|bass boosted|reaction|tutorial|piano version|extended|full album|1 hour|10 hours|mix)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SpacesRx = new(@"\s+", RegexOptions.Compiled);

        private static readonly string[] GlobalDiscoverySeeds = new[]
        {
            "Billboard Hot 100 hits playlist",
            "Top global trending viral hits",
            "Top alternative indie songs",
            "Pop rock essentials playlist",
            "Top hits 2026 trending songs",
            "Chill acoustic and indie favorites"
        };

        public RecommendationService(
            ICurrentUserService currentUserService,
            IHistoryRepository historyRepository,
            IFavoriteRepository favoriteRepository,
            ITrackRepository trackRepository,
            IMusicProviderManager providerManager,
            ITrackResolver trackResolver,
            IMemoryCache cache)
        {
            this.currentUserService = currentUserService;
            this.historyRepository = historyRepository;
            this.favoriteRepository = favoriteRepository;
            this.trackRepository = trackRepository;
            this.providerManager = providerManager;
            this.trackResolver = trackResolver;
            this.cache = cache;
        }

        public async Task<RecommendationResultDto> GetRecommendationsAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            var userId = currentUserService.UserId;
            var isAuthenticated = userId != Guid.Empty;

            if (isAuthenticated)
            {
                var cacheKey = $"recommendations_user_{userId}";
                if (cache.TryGetValue(cacheKey, out RecommendationResultDto? cached) && cached != null)
                {
                    return cached;
                }

                var personalized = await TryGetPersonalizedRecommendationsAsync(userId, limit, cancellationToken);
                if (personalized != null && personalized.Tracks.Count >= 4)
                {
                    cache.Set(cacheKey, personalized, TimeSpan.FromHours(1));
                    return personalized;
                }
            }

            var seedIndex = DateTime.UtcNow.Hour % GlobalDiscoverySeeds.Length;
            var globalCacheKey = $"recommendations_global_seed_{seedIndex}";
            if (cache.TryGetValue(globalCacheKey, out RecommendationResultDto? cachedGlobal) && cachedGlobal != null)
            {
                return cachedGlobal;
            }

            var globalResult = await GetGlobalFallbackRecommendationsAsync(limit, seedIndex, cancellationToken);
            cache.Set(globalCacheKey, globalResult, TimeSpan.FromHours(2));
            return globalResult;
        }

        public async Task<IReadOnlyList<TrackDto>> GetRadioTracksAsync(Guid trackId, int limit = 8, CancellationToken cancellationToken = default)
        {
            var currentTrack = await trackRepository.GetByIdAsync(trackId, cancellationToken);
            if (currentTrack == null)
                return [];

            var cacheKey = $"radio_track_{trackId}_{limit}";
            if (cache.TryGetValue(cacheKey, out IReadOnlyList<TrackDto>? cached) && cached != null && cached.Count > 0)
                return cached;

            var currentExternalIds = currentTrack.ProviderTracks
                .Select(pt => pt.ExternalId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var currentArtistName = currentTrack.Artist?.Name?.Trim() ?? string.Empty;
            var currentTitle = currentTrack.Title?.Trim() ?? string.Empty;
            var currentKey = SongKey(currentTitle, currentArtistName);

            var seenKeys = new HashSet<string>(StringComparer.Ordinal) { currentKey };
            var userId = currentUserService.UserId;
            if (userId != Guid.Empty)
            {
                try
                {
                    var recent = await historyRepository.GetRecentByUserIdAsync(userId, 30, cancellationToken);
                    foreach (var h in recent)
                    {
                        if (!string.IsNullOrWhiteSpace(h.Track?.Title))
                            seenKeys.Add(SongKey(h.Track!.Title, h.Track.Artist?.Name));
                    }
                }
                catch { /* istoricul e optional */ }
            }

            var chosen = new List<TrackCandidate>();
            var seenExternalIds = new HashSet<string>(currentExternalIds, StringComparer.OrdinalIgnoreCase);
            var seenTitles = new List<string> { currentTitle };
            var artistCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            bool TryAdd(TrackCandidate c, int maxPerArtist)
            {
                if (string.IsNullOrWhiteSpace(c.Title) || seenExternalIds.Contains(c.ExternalId))
                    return false;
                if (!TrackTitleSanitizer.IsReasonableDuration(c.Duration))
                    return false;
                if (TrackTitleSanitizer.IsCompilationOrNoise(c.Title, c.Duration))
                    return false;

                var key = SongKey(c.Title, c.Artist);
                if (key.Length == 0 || seenKeys.Contains(key))
                    return false;

                if (TrackTitleSanitizer.IsSameSongOrVariation(c.Title, currentTitle))
                    return false;
                if (seenTitles.Any(t => TrackTitleSanitizer.IsSameSongOrVariation(c.Title, t)))
                    return false;

                var artist = string.IsNullOrWhiteSpace(c.Artist) ? "Unknown" : c.Artist.Trim();
                var count = artistCount.GetValueOrDefault(artist, 0);
                if (count >= maxPerArtist)
                    return false;

                seenExternalIds.Add(c.ExternalId);
                seenKeys.Add(key);
                seenTitles.Add(c.Title);
                artistCount[artist] = count + 1;
                chosen.Add(c);
                return true;
            }

            var targets = await GetDeezerTargetsAsync(currentArtistName, currentTitle, currentKey, limit + 3, cancellationToken);

            if (targets.Count >= 3)
            {
                using var gate = new SemaphoreSlim(4);
                var lookups = targets.Select(async t =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        var results = await providerManager.SearchAsync($"{t.Artist} - {t.Title}", cancellationToken);
                        return results.FirstOrDefault(c => MatchesTarget(c, t.Artist, t.Title));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { return null; }
                    finally { gate.Release(); }
                }).ToList();

                var found = await Task.WhenAll(lookups);
                foreach (var cand in found)
                {
                    if (cand == null) continue;
                    TryAdd(cand, maxPerArtist: 2);
                    if (chosen.Count >= limit) break;
                }
            }

            if (chosen.Count < limit && !string.IsNullOrWhiteSpace(currentArtistName))
            {
                try
                {
                    var results = await providerManager.SearchAsync($"{currentArtistName} top songs", cancellationToken);
                    foreach (var cand in results)
                    {
                        if (chosen.Count >= limit) break;
                        TryAdd(cand, maxPerArtist: 2);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }

            if (chosen.Count == 0)
                return [];

            var reordered = InterleaveCandidates(chosen, currentArtistName);
            var resolvedTracks = await trackResolver.ResolveBatchAsync(reordered, cancellationToken);
            var resultDtos = resolvedTracks.Select(MapToDto).ToList();

            var ttl = resultDtos.Count >= limit ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(2);
            cache.Set(cacheKey, (IReadOnlyList<TrackDto>)resultDtos, ttl);
            return resultDtos;
        }

        private async Task<RecommendationResultDto> GetGlobalFallbackRecommendationsAsync(int limit, int seedIndex, CancellationToken cancellationToken)
        {
            try
            {
                var topTracks = await historyRepository.GetTopTracksAsync(limit, cancellationToken);
                if (topTracks.Count >= 5)
                {
                    return new RecommendationResultDto
                    {
                        Title = "Popular on Stream",
                        Subtitle = "Trending tracks listened to by the community",
                        IsPersonalized = false,
                        Tracks = topTracks.Select(MapToDto).ToList()
                    };
                }
            }
            catch
            {
            }

            try
            {
                var seedQuery = GlobalDiscoverySeeds[seedIndex % GlobalDiscoverySeeds.Length];
                var candidates = await providerManager.SearchAsync(seedQuery, cancellationToken);

                var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenTitles = new List<string>();
                var artistCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var diverseCandidates = new List<TrackCandidate>();

                foreach (var candidate in candidates)
                {
                    if (seenExternalIds.Contains(candidate.ExternalId))
                        continue;

                    if (!TrackTitleSanitizer.IsReasonableDuration(candidate.Duration))
                        continue;

                    if (seenTitles.Any(t => TrackTitleSanitizer.IsSameSongOrVariation(candidate.Title, t)))
                        continue;

                    var candidateArtist = string.IsNullOrWhiteSpace(candidate.Artist) ? "Unknown" : candidate.Artist.Trim();
                    if (artistCount.GetValueOrDefault(candidateArtist, 0) >= 2)
                        continue;

                    seenExternalIds.Add(candidate.ExternalId);
                    seenTitles.Add(candidate.Title);
                    artistCount[candidateArtist] = artistCount.GetValueOrDefault(candidateArtist, 0) + 1;
                    diverseCandidates.Add(candidate);

                    if (diverseCandidates.Count >= limit)
                        break;
                }

                var resolved = await trackResolver.ResolveBatchAsync(diverseCandidates, cancellationToken);

                return new RecommendationResultDto
                {
                    Title = "Trending Hits",
                    Subtitle = "Popular songs right now",
                    IsPersonalized = false,
                    Tracks = resolved.Select(MapToDto).ToList()
                };
            }
            catch
            {
                return new RecommendationResultDto
                {
                    Title = "Suggested Songs",
                    Subtitle = "Discover music",
                    IsPersonalized = false,
                    Tracks = []
                };
            }
        }

        private static List<TrackCandidate> InterleaveCandidates(List<TrackCandidate> candidates, string primaryArtist)
        {
            if (candidates.Count <= 2 || string.IsNullOrWhiteSpace(primaryArtist))
                return candidates;

            var primary = candidates.Where(c => string.Equals(c.Artist, primaryArtist, StringComparison.OrdinalIgnoreCase)).ToList();
            var others = candidates.Where(c => !string.Equals(c.Artist, primaryArtist, StringComparison.OrdinalIgnoreCase)).ToList();

            var result = new List<TrackCandidate>();
            if (primary.Count > 0)
            {
                result.Add(primary[0]);
            }

            var otherIndex = 0;
            while (otherIndex < others.Count)
            {
                result.Add(others[otherIndex]);
                otherIndex++;

                if (primary.Count > 1 && result.Count == 3)
                {
                    result.Add(primary[1]);
                }
            }

            if (primary.Count > 1 && !result.Contains(primary[1]))
            {
                result.Add(primary[1]);
            }

            return result;
        }

        private static TrackDto MapToDto(Track track)
        {
            return new TrackDto
            {
                Id = track.Id,
                Title = track.Title,
                ArtistName = track.Artist?.Name ?? string.Empty,
                Duration = track.Duration,
                ArtworkUrl = track.ArtworkUrl,
                Providers = track.ProviderTracks.Select(pt => new ProviderTrackDto
                {
                    Provider = pt.Provider,
                    ExternalId = pt.ExternalId,
                    ExternalUrl = pt.ExternalUrl
                }).ToList()
            };
        }

        private static bool MatchesTarget(TrackCandidate c, string targetArtist, string targetTitle)
        {
            if (string.IsNullOrWhiteSpace(c.Title)) return false;
            if (!TrackTitleSanitizer.IsReasonableDuration(c.Duration)) return false;
            if (TrackTitleSanitizer.IsCompilationOrNoise(c.Title, c.Duration)) return false;

            if (VariationRx.IsMatch(c.Title) && !VariationRx.IsMatch(targetTitle)) return false;

            var wantedTitle = SongKey(targetTitle, targetArtist);
            if (wantedTitle.Length == 0) return false;
            if (!SongKey(c.Title, targetArtist).Contains(wantedTitle, StringComparison.Ordinal)) return false;

            var wantedArtist = Norm(targetArtist);
            return wantedArtist.Length == 0 || Norm($"{c.Artist} {c.Title}").Contains(wantedArtist, StringComparison.Ordinal);
        }

        private async Task<List<(string Artist, string Title)>> GetDeezerTargetsAsync(
            string artistName, string currentTitle, string currentKey, int wanted, CancellationToken ct)
        {
            var targets = new List<(string Artist, string Title)>();
            if (string.IsNullOrWhiteSpace(artistName))
                return targets;

            var keys = new HashSet<string>(StringComparer.Ordinal) { currentKey };
            var perArtist = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            bool AddTarget(string artist, string title, int maxPerArtist, int? insertAt = null)
            {
                if (TrackTitleSanitizer.IsSameSongOrVariation(title, currentTitle)) return false;
                var key = SongKey(title, artist);
                if (key.Length == 0 || !keys.Add(key)) return false;
                var n = perArtist.GetValueOrDefault(artist, 0);
                if (n >= maxPerArtist) { keys.Remove(key); return false; }
                perArtist[artist] = n + 1;
                if (insertAt.HasValue) targets.Insert(Math.Min(insertAt.Value, targets.Count), (artist, title));
                else targets.Add((artist, title));
                return true;
            }

            var searchJson = await TryGetDeezerAsync(
                $"https://api.deezer.com/search/artist?q={Uri.EscapeDataString(artistName)}&limit=5", ct);
            if (searchJson == null) return targets;

            long? artistId = null;
            try
            {
                using var doc = JsonDocument.Parse(searchJson);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (!item.TryGetProperty("id", out var idEl)) continue;
                        var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                        artistId ??= idEl.GetInt64();
                        if (Norm(name) == Norm(artistName)) { artistId = idEl.GetInt64(); break; }
                    }
                }
            }
            catch (JsonException) { }
            if (artistId == null) return targets;

            var radioTask = TryGetDeezerAsync($"https://api.deezer.com/artist/{artistId}/radio", ct);
            var topTask = TryGetDeezerAsync($"https://api.deezer.com/artist/{artistId}/top?limit=15", ct);
            var relatedTask = TryGetDeezerAsync($"https://api.deezer.com/artist/{artistId}/related?limit=10", ct);
            await Task.WhenAll(radioTask, topTask, relatedTask);

            var own = ParseDeezerTracks(topTask.Result)
                .Where(t => !TrackTitleSanitizer.IsSameSongOrVariation(t.Title, currentTitle))
                .ToList();
            var radio = ParseDeezerTracks(radioTask.Result);

            if (own.Count > 0) AddTarget(artistName, own[0].Title, 2);

            foreach (var t in radio)
            {
                if (targets.Count >= wanted) break;
                AddTarget(t.Artist, t.Title, 2);
            }

            if (targets.Count < wanted)
            {
                var related = new List<(long Id, string Name)>();
                try
                {
                    if (relatedTask.Result != null)
                    {
                        using var doc = JsonDocument.Parse(relatedTask.Result);
                        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                            foreach (var item in data.EnumerateArray().Take(5))
                                if (item.TryGetProperty("id", out var id) && item.TryGetProperty("name", out var nm))
                                    related.Add((id.GetInt64(), nm.GetString() ?? ""));
                    }
                }
                catch (JsonException) { }

                var relatedTops = await Task.WhenAll(related.Select(r =>
                    TryGetDeezerAsync($"https://api.deezer.com/artist/{r.Id}/top?limit=3", ct)));

                foreach (var json in relatedTops)
                    foreach (var t in ParseDeezerTracks(json).Take(2))
                    {
                        if (targets.Count >= wanted) break;
                        AddTarget(t.Artist, t.Title, 2);
                    }
            }

            if (own.Count > 1 && perArtist.GetValueOrDefault(artistName, 0) < 2)
                AddTarget(artistName, own[1].Title, 2, insertAt: 3);

            return targets;
        }

        private static async Task<string?> TryGetDeezerAsync(string url, CancellationToken ct)
        {
            try { return await DeezerClient.GetStringAsync(url, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                Console.WriteLine($"[RecommendationService] Deezer error ({url}): {ex.Message}");
                return null;
            }
        }

        private static List<(string Artist, string Title)> ParseDeezerTracks(string? json)
        {
            var list = new List<(string Artist, string Title)>();
            if (json == null) return list;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    return list;

                foreach (var item in data.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                    string? artist = null;
                    if (item.TryGetProperty("artist", out var a) && a.TryGetProperty("name", out var n))
                        artist = n.GetString();

                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
                        list.Add((artist.Trim(), title.Trim()));
                }
            }
            catch (JsonException) { }
            return list;
        }

        private static string Norm(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var ch in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
            }
            return SpacesRx.Replace(sb.ToString(), " ").Trim();
        }

        private static string SongKey(string? title, string? artist = null)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            var t = BracketsRx.Replace(title, " ");
            t = FeatRx.Replace(t, " ");
            t = DashSuffixRx.Replace(t, " ");
            var key = NoiseWordsRx.Replace(Norm(t), " ");

            var a = Norm(artist);
            if (a.Length > 0)
                key = Regex.Replace(key, $@"\b{Regex.Escape(a)}\b", " ");

            return SpacesRx.Replace(key, " ").Trim();
        }

        private async Task<RecommendationResultDto?> TryGetPersonalizedRecommendationsAsync(Guid userId, int limit, CancellationToken cancellationToken)
        {
            var recentHistory = await historyRepository.GetRecentByUserIdAsync(userId, 40, cancellationToken);
            var favorites = await favoriteRepository.GetFavoritesTracksByUserIdAsync(userId, cancellationToken);

            if (recentHistory.Count == 0 && favorites.Count == 0)
                return null;

            var excludedTrackIds = new HashSet<Guid>();
            var excludedRawTitles = new List<string>();

            foreach (var h in recentHistory)
            {
                if (h.Track != null)
                {
                    excludedTrackIds.Add(h.TrackId);
                    if (!string.IsNullOrWhiteSpace(h.Track.Title))
                        excludedRawTitles.Add(h.Track.Title.Trim());
                }
            }

            foreach (var f in favorites)
            {
                excludedTrackIds.Add(f.Id);
                if (!string.IsNullOrWhiteSpace(f.Title))
                    excludedRawTitles.Add(f.Title.Trim());
            }

            var artistFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var h in recentHistory)
            {
                var artistName = h.Track?.Artist?.Name?.Trim();
                if (!string.IsNullOrWhiteSpace(artistName))
                {
                    artistFrequency[artistName] = artistFrequency.GetValueOrDefault(artistName) + 2;
                }
            }

            foreach (var f in favorites)
            {
                var artistName = f.Artist?.Name?.Trim();
                if (!string.IsNullOrWhiteSpace(artistName))
                {
                    artistFrequency[artistName] = artistFrequency.GetValueOrDefault(artistName) + 3;
                }
            }

            var topArtists = artistFrequency
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .Take(2)
                .ToList();

            if (topArtists.Count == 0)
                return null;

            var candidatePool = new List<TrackCandidate>();
            var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenTitles = new List<string>(excludedRawTitles);
            var artistCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var artistSearchTasks = topArtists.Select(async artist =>
            {
                try
                {
                    var results = await providerManager.SearchAsync($"{artist} top songs", cancellationToken);
                    return (Artist: artist, Candidates: results);
                }
                catch
                {
                    return (Artist: artist, Candidates: (IReadOnlyList<TrackCandidate>)[]);
                }
            }).ToList();

            Task<(string Artist, IReadOnlyList<TrackCandidate> Candidates)>? discoveryTask = null;
            if (topArtists.Count == 1)
            {
                discoveryTask = Task.Run(async () =>
                {
                    try
                    {
                        var results = await providerManager.SearchAsync($"{topArtists[0]} similar artists mix", cancellationToken);
                        return (Artist: "Discovery", Candidates: results);
                    }
                    catch
                    {
                        return (Artist: "Discovery", Candidates: (IReadOnlyList<TrackCandidate>)[]);
                    }
                }, cancellationToken);
            }

            var allTasks = new List<Task>();
            allTasks.AddRange(artistSearchTasks);
            if (discoveryTask != null)
                allTasks.Add(discoveryTask);

            try
            {
                await Task.WhenAll(allTasks);
            }
            catch
            {
            }

            foreach (var task in artistSearchTasks)
            {
                if (!task.IsCompletedSuccessfully)
                    continue;

                var (artist, searchResults) = task.Result;
                var addedForThisArtist = 0;

                foreach (var candidate in searchResults)
                {
                    if (seenExternalIds.Contains(candidate.ExternalId))
                        continue;

                    if (!TrackTitleSanitizer.IsReasonableDuration(candidate.Duration))
                        continue;

                    if (seenTitles.Any(t => TrackTitleSanitizer.IsSameSongOrVariation(candidate.Title, t)))
                        continue;

                    var candidateArtist = string.IsNullOrWhiteSpace(candidate.Artist) ? artist : candidate.Artist.Trim();
                    if (artistCount.GetValueOrDefault(candidateArtist, 0) >= 2)
                        continue;

                    seenExternalIds.Add(candidate.ExternalId);
                    seenTitles.Add(candidate.Title);
                    artistCount[candidateArtist] = artistCount.GetValueOrDefault(candidateArtist, 0) + 1;
                    candidatePool.Add(candidate);
                    addedForThisArtist++;

                    if (addedForThisArtist >= 2)
                        break;
                }
            }

            if (discoveryTask != null && discoveryTask.IsCompletedSuccessfully && candidatePool.Count < limit * 2)
            {
                var (_, discoveryResults) = discoveryTask.Result;
                foreach (var candidate in discoveryResults)
                {
                    if (seenExternalIds.Contains(candidate.ExternalId))
                        continue;

                    if (!TrackTitleSanitizer.IsReasonableDuration(candidate.Duration))
                        continue;

                    if (seenTitles.Any(t => TrackTitleSanitizer.IsSameSongOrVariation(candidate.Title, t)))
                        continue;

                    var candidateArtist = string.IsNullOrWhiteSpace(candidate.Artist) ? "Unknown" : candidate.Artist.Trim();
                    if (artistCount.GetValueOrDefault(candidateArtist, 0) >= 2)
                        continue;

                    seenExternalIds.Add(candidate.ExternalId);
                    seenTitles.Add(candidate.Title);
                    artistCount[candidateArtist] = artistCount.GetValueOrDefault(candidateArtist, 0) + 1;
                    candidatePool.Add(candidate);

                    if (candidatePool.Count >= limit * 2)
                        break;
                }
            }

            if (candidatePool.Count == 0)
                return null;

            IReadOnlyList<Track> resolvedTracks = [];
            try
            {
                resolvedTracks = await trackResolver.ResolveBatchAsync(candidatePool.Take(limit * 2).ToList(), cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecommendationService] Error resolving personalized tracks batch: {ex.Message}");
            }

            var filteredTracks = resolvedTracks
                .Where(t => !excludedTrackIds.Contains(t.Id))
                .Take(limit)
                .Select(MapToDto)
                .ToList();

            if (filteredTracks.Count == 0)
                return null;

            var subtitle = topArtists.Count == 1
                ? $"Because you like {topArtists[0]}"
                : $"Based on your interest in {string.Join(", ", topArtists.Take(2))}";

            return new RecommendationResultDto
            {
                Title = "Recommended for You",
                Subtitle = subtitle,
                IsPersonalized = true,
                Tracks = filteredTracks
            };
        }
    }
}
