using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class TrackResolver : ITrackResolver
    {
        private readonly ITrackRepository trackRepository;
        private readonly IArtistRepository artistRepository;
        private readonly IProviderTrackRepository providerTrackRepository;
        private readonly IUnitOfWork unitOfWork;

        public TrackResolver(ITrackRepository trackRepository, IArtistRepository artistRepository, IProviderTrackRepository providerTrackRepository, IUnitOfWork unitOfWork)
        {
            this.trackRepository = trackRepository;
            this.artistRepository = artistRepository;
            this.providerTrackRepository = providerTrackRepository;
            this.unitOfWork = unitOfWork;
        }

        private static string Truncate(string? value, int maxLength = 250)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private static string SafeArtistName(string? artist)
        {
            var truncated = Truncate(artist, 250);
            return string.IsNullOrWhiteSpace(truncated) ? "Unknown Artist" : truncated;
        }

        private static string SafeTitle(string? title)
        {
            var truncated = Truncate(title, 250);
            return string.IsNullOrWhiteSpace(truncated) ? "Untitled Track" : truncated;
        }

        private static string SafeArtworkUrl(string? artworkUrl, string? externalId, string provider)
        {
            if (string.IsNullOrWhiteSpace(artworkUrl)
                || artworkUrl.Contains("invidious", StringComparison.OrdinalIgnoreCase)
                || artworkUrl.StartsWith("/vi/", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(externalId) && (provider.Equals("Invidious", StringComparison.OrdinalIgnoreCase) || provider.Equals("Youtube", StringComparison.OrdinalIgnoreCase) || externalId.Length == 11))
                {
                    return $"https://i.ytimg.com/vi/{externalId}/hqdefault.jpg";
                }
            }
            return artworkUrl ?? string.Empty;
        }

        public async Task<Track> ResolveAsync(TrackCandidate candidate, CancellationToken cancellationToken)
        {
            var existingProviderTrack = await providerTrackRepository
                .FindByProviderAndExternalIdAsync(candidate.Provider, candidate.ExternalId, cancellationToken);

            if (existingProviderTrack != null)
            {
                if (!string.IsNullOrWhiteSpace(existingProviderTrack.Track.ArtworkUrl) &&
                    (existingProviderTrack.Track.ArtworkUrl.Contains("invidious", StringComparison.OrdinalIgnoreCase) || existingProviderTrack.Track.ArtworkUrl.StartsWith("/vi/")))
                {
                    existingProviderTrack.Track.ArtworkUrl = SafeArtworkUrl(existingProviderTrack.Track.ArtworkUrl, existingProviderTrack.ExternalId, existingProviderTrack.Provider);
                }
                return existingProviderTrack.Track;
            }

            var safeTitle = SafeTitle(candidate.Title);
            var safeArtist = SafeArtistName(candidate.Artist);

            var track = await trackRepository.FindByTitleAndArtistAsync(
                safeTitle,
                safeArtist,
                cancellationToken);

            if (track == null)
            {
                var artist = await artistRepository.FindByNameAsync(safeArtist, cancellationToken);

                if (artist == null)
                {
                    artist = new Artist
                    {
                        Id = Guid.NewGuid(),
                        Name = safeArtist
                    };

                    try
                    {
                        await artistRepository.AddAsync(artist, cancellationToken);
                    }
                    catch
                    {
                        artist = await artistRepository.FindByNameAsync(safeArtist, cancellationToken) ?? artist;
                    }
                }

                track = new Track
                {
                    Id = Guid.NewGuid(),
                    Title = safeTitle,
                    ArtistId = artist.Id,
                    Artist = artist,
                    Duration = candidate.Duration ?? TimeSpan.Zero,
                    ArtworkUrl = SafeArtworkUrl(candidate.ArtworkUrl, candidate.ExternalId, candidate.Provider),
                };

                await trackRepository.AddAsync(track, cancellationToken);
            }
            
            var providerTrack = new ProviderTrack
            {
                Id = Guid.NewGuid(),
                TrackId = track.Id,
                Track = track,
                Provider = candidate.Provider,
                ExternalId = candidate.ExternalId,
                ExternalUrl = candidate.ExternalUrl,
                IsPlayable = true,
            };

            try
            {
                await providerTrackRepository.AddAsync(providerTrack, cancellationToken);
                track.ProviderTracks.Add(providerTrack);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrackResolver] ProviderTrack already exists or write conflict: {ex.Message}");
                var reloaded = await providerTrackRepository
                    .FindByProviderAndExternalIdAsync(candidate.Provider, candidate.ExternalId, cancellationToken);
                if (reloaded != null)
                    return reloaded.Track;
            }

            return track;
        }

        public async Task<IReadOnlyList<Track>> ResolveBatchAsync(IReadOnlyList<TrackCandidate> candidates, CancellationToken cancellationToken)
        {
            if (candidates == null || candidates.Count == 0)
                return [];

            var resultsMap = new Dictionary<string, Track>();
            var providersGroup = candidates.GroupBy(c => c.Provider);

            foreach (var group in providersGroup)
            {
                var providerName = group.Key;
                var groupCandidates = group.ToList();
                var externalIds = groupCandidates.Select(c => c.ExternalId).Distinct().ToList();

                var existingProviderTracks = await providerTrackRepository
                    .FindByExternalIdsAsync(providerName, externalIds, cancellationToken);

                var existingMap = existingProviderTracks
                    .GroupBy(pt => pt.ExternalId)
                    .ToDictionary(g => g.Key, g => g.First());

                var missingCandidates = groupCandidates
                    .Where(c => !existingMap.ContainsKey(c.ExternalId))
                    .GroupBy(c => c.ExternalId)
                    .Select(g => g.First())
                    .ToList();

                if (missingCandidates.Count > 0)
                {
                    // 1. Interogăm artiștii lipsă într-un singur SELECT
                    var artistNames = missingCandidates
                        .Select(c => SafeArtistName(c.Artist))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var existingArtists = await artistRepository.FindByNamesAsync(artistNames, cancellationToken);
                    var artistMap = existingArtists
                        .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    var newArtists = new List<Artist>();
                    foreach (var name in artistNames)
                    {
                        if (!artistMap.ContainsKey(name))
                        {
                            var newArtist = new Artist
                            {
                                Id = Guid.NewGuid(),
                                Name = name
                            };
                            newArtists.Add(newArtist);
                            artistMap[name] = newArtist;
                        }
                    }

                    if (newArtists.Count > 0)
                    {
                        try
                        {
                            await artistRepository.AddRangeAsync(newArtists, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TrackResolver] Artist add warning: {ex.Message}");
                        }
                    }

                    var newTracks = new List<Track>();
                    foreach (var candidate in missingCandidates)
                    {
                        var artistName = SafeArtistName(candidate.Artist);
                        var trackTitle = SafeTitle(candidate.Title);

                        var artist = artistMap.TryGetValue(artistName, out var a)
                            ? a
                            : new Artist { Id = Guid.NewGuid(), Name = artistName };

                        var track = new Track
                        {
                            Id = Guid.NewGuid(),
                            Title = trackTitle,
                            ArtistId = artist.Id,
                            Artist = artist,
                            Duration = candidate.Duration ?? TimeSpan.Zero,
                            ArtworkUrl = SafeArtworkUrl(candidate.ArtworkUrl, candidate.ExternalId, candidate.Provider),
                            ProviderTracks = new List<ProviderTrack>()
                        };

                        var providerTrack = new ProviderTrack
                        {
                            Id = Guid.NewGuid(),
                            TrackId = track.Id,
                            Track = track,
                            Provider = candidate.Provider,
                            ExternalId = candidate.ExternalId,
                            ExternalUrl = candidate.ExternalUrl,
                            IsPlayable = true
                        };

                        track.ProviderTracks.Add(providerTrack);
                        newTracks.Add(track);
                        existingMap[candidate.ExternalId] = providerTrack;
                    }

                    try
                    {
                        await trackRepository.AddRangeAsync(newTracks, cancellationToken);
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TrackResolver] Batch save conflict (handled): {ex.Message}");
                    }
                }

                foreach (var candidate in groupCandidates)
                {
                    if (existingMap.TryGetValue(candidate.ExternalId, out var pt))
                    {
                        if (!string.IsNullOrWhiteSpace(pt.Track.ArtworkUrl) &&
                            (pt.Track.ArtworkUrl.Contains("invidious", StringComparison.OrdinalIgnoreCase) || pt.Track.ArtworkUrl.StartsWith("/vi/")))
                        {
                            pt.Track.ArtworkUrl = SafeArtworkUrl(pt.Track.ArtworkUrl, pt.ExternalId, pt.Provider);
                        }
                        resultsMap[$"{candidate.Provider}_{candidate.ExternalId}"] = pt.Track;
                    }
                }
            }

            var orderedResults = new List<Track>();
            foreach (var candidate in candidates)
            {
                var key = $"{candidate.Provider}_{candidate.ExternalId}";
                if (resultsMap.TryGetValue(key, out var track))
                {
                    orderedResults.Add(track);
                }
            }
            return orderedResults;
        }
    }
}
