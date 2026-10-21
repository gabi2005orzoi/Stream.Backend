using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Stream.Infrastructure.Providers
{
    public class InvidiousProvider : IMusicProvider
    {
        public string Name => "Invidious";

        private readonly HttpClient httpClient;
        private readonly List<string> instances;

        public InvidiousProvider(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;

            var configuredInstance = configuration["INVIDIOUS_INSTANCE"] 
                ?? configuration["Invidious:InstanceUrl"] 
                ?? Environment.GetEnvironmentVariable("INVIDIOUS_INSTANCE");

            instances = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredInstance))
            {
                instances.Add(configuredInstance.TrimEnd('/'));
            }

            // Reliable default public instances with active API
            var defaults = new[]
            {
                "https://invidious.f5.si",
                "https://inv.nadeko.net",
                "https://invidious.nerdvpn.de",
                "https://invidious.tiekoetter.com"
            };

            foreach (var d in defaults)
            {
                if (!instances.Contains(d, StringComparer.OrdinalIgnoreCase))
                {
                    instances.Add(d);
                }
            }
        }

        public async Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            foreach (var instance in instances)
            {
                try
                {
                    var url = $"{instance}/api/v1/search?q={Uri.EscapeDataString(query.Trim())}&type=video";
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(5));

                    var response = await httpClient.GetAsync(url, cts.Token);
                    if (!response.IsSuccessStatusCode)
                        continue;

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.ValueKind != JsonValueKind.Array)
                        continue;

                    var results = new List<TrackCandidate>();
                    foreach (var item in root.EnumerateArray())
                    {
                        var videoId = item.TryGetProperty("videoId", out var vId) ? vId.GetString() : null;
                        if (string.IsNullOrWhiteSpace(videoId))
                            continue;

                        var title = item.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? "Unknown" : "Unknown";
                        var author = item.TryGetProperty("author", out var aProp) ? aProp.GetString() ?? "Unknown Artist" : "Unknown Artist";

                        TimeSpan? duration = null;
                        if (item.TryGetProperty("lengthSeconds", out var lenProp) && lenProp.TryGetInt32(out var sec))
                        {
                            duration = TimeSpan.FromSeconds(sec);
                        }

                        string? artworkUrl = null;
                        if (item.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.ValueKind == JsonValueKind.Array)
                        {
                            var bestThumb = thumbs.EnumerateArray()
                                .Where(th => th.TryGetProperty("url", out _))
                                .OrderByDescending(th => th.TryGetProperty("width", out var w) ? w.GetInt32() : 0)
                                .FirstOrDefault();

                            if (bestThumb.ValueKind == JsonValueKind.Object && bestThumb.TryGetProperty("url", out var uProp))
                            {
                                artworkUrl = uProp.GetString();
                            }
                        }

                        results.Add(new TrackCandidate
                        {
                            Provider = Name,
                            ExternalId = videoId,
                            Title = title,
                            Artist = author,
                            Album = null,
                            Duration = duration,
                            ArtworkUrl = artworkUrl,
                            ExternalUrl = $"https://youtube.com/watch?v={videoId}"
                        });

                        if (results.Count >= 20)
                            break;
                    }

                    if (results.Count > 0)
                        return results;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[InvidiousProvider] Search failed on instance {instance}: {ex.Message}");
                }
            }

            return [];
        }

        public async Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return null;

            foreach (var instance in instances)
            {
                try
                {
                    var url = $"{instance}/api/v1/videos/{Uri.EscapeDataString(externalId)}";
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(6));

                    var response = await httpClient.GetAsync(url, cts.Token);
                    if (!response.IsSuccessStatusCode)
                        continue;

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("adaptiveFormats", out var formats) || formats.ValueKind != JsonValueKind.Array)
                        continue;

                    string? bestAudioUrl = null;
                    string mimeType = "audio/mp4";
                    long highestBitrate = 0;

                    foreach (var f in formats.EnumerateArray())
                    {
                        var type = f.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "" : "";
                        if (!type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var streamUrl = f.TryGetProperty("url", out var uProp) ? uProp.GetString() : null;
                        if (string.IsNullOrWhiteSpace(streamUrl))
                            continue;

                        long bitrate = 0;
                        if (f.TryGetProperty("bitrate", out var bProp))
                        {
                            if (bProp.ValueKind == JsonValueKind.Number)
                                bitrate = bProp.GetInt64();
                            else if (bProp.ValueKind == JsonValueKind.String && long.TryParse(bProp.GetString(), out var parsedBitrate))
                                bitrate = parsedBitrate;
                        }

                        if (bitrate > highestBitrate || bestAudioUrl == null)
                        {
                            highestBitrate = bitrate;
                            bestAudioUrl = streamUrl;
                            mimeType = type.Split(';')[0].Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(bestAudioUrl))
                    {
                        return new PlaybackSource
                        {
                            Provider = Name,
                            Url = bestAudioUrl,
                            MimeType = mimeType,
                            ExpiresAt = DateTime.UtcNow.AddHours(5)
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[InvidiousProvider] GetPlaybackSource failed on {instance} for {externalId}: {ex.Message}");
                }
            }

            return null;
        }

        public async Task<AudioStreamResult?> GetAudioStreamAsync(string externalId, CancellationToken cancellationToken = default)
        {
            var playbackSource = await GetPlaybackSourceAsync(externalId, cancellationToken);
            if (playbackSource == null || string.IsNullOrWhiteSpace(playbackSource.Url))
                return null;

            try
            {
                var response = await httpClient.GetAsync(playbackSource.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? playbackSource.MimeType ?? "audio/mpeg";

                return new AudioStreamResult
                {
                    Stream = stream,
                    MimeType = mediaType
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InvidiousProvider] Failed streaming audio for {externalId}: {ex.Message}");
                return null;
            }
        }
    }
}
