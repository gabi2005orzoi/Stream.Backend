using Stream.Application.DTOs;
using Stream.Application.Interfaces;
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
    public class AudiusProvider : IMusicProvider
    {
        public string Name => "Audius";

        private readonly HttpClient httpClient;
        private const string AppName = "StreamApp";
        private const string BaseApiUrl = "https://api.audius.co/v1";

        public AudiusProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var results = new List<TrackCandidate>();

            try
            {
                var url = $"{BaseApiUrl}/tracks/search?query={Uri.EscapeDataString(query.Trim())}&app_name={AppName}";
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AudiusProvider] Search returned HTTP {response.StatusCode}");
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array)
                    return [];

                foreach (var item in dataArray.EnumerateArray())
                {
                    // Only include streamable tracks
                    if (item.TryGetProperty("is_streamable", out var isStreamableProp) && !isStreamableProp.GetBoolean())
                        continue;

                    string? id = null;
                    if (item.TryGetProperty("id", out var idProp))
                    {
                        id = idProp.ValueKind == JsonValueKind.String ? idProp.GetString() : idProp.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    var title = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Unknown Title" : "Unknown Title";

                    var artist = "Unknown Artist";
                    if (item.TryGetProperty("user", out var userProp) && userProp.ValueKind == JsonValueKind.Object)
                    {
                        if (userProp.TryGetProperty("name", out var nameProp))
                            artist = nameProp.GetString() ?? artist;
                    }

                    TimeSpan? duration = null;
                    if (item.TryGetProperty("duration", out var durProp) && durProp.TryGetInt32(out var durSec))
                    {
                        duration = TimeSpan.FromSeconds(durSec);
                    }

                    string? artworkUrl = null;
                    if (item.TryGetProperty("artwork", out var artProp) && artProp.ValueKind == JsonValueKind.Object)
                    {
                        if (artProp.TryGetProperty("480x480", out var art480))
                            artworkUrl = art480.GetString();
                        else if (artProp.TryGetProperty("150x150", out var art150))
                            artworkUrl = art150.GetString();
                        else if (artProp.TryGetProperty("1000x1000", out var art1000))
                            artworkUrl = art1000.GetString();
                    }

                    string? externalUrl = null;
                    if (item.TryGetProperty("permalink", out var permProp))
                    {
                        var perm = permProp.GetString();
                        if (!string.IsNullOrWhiteSpace(perm))
                            externalUrl = perm.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? perm : $"https://audius.co{perm}";
                    }

                    results.Add(new TrackCandidate
                    {
                        Provider = Name,
                        ExternalId = id,
                        Title = title.Trim(),
                        Artist = artist.Trim(),
                        Album = null,
                        Duration = duration,
                        ArtworkUrl = artworkUrl,
                        ExternalUrl = externalUrl
                    });

                    if (results.Count >= 20)
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudiusProvider] Search error: {ex.Message}");
            }

            return results;
        }

        public Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return Task.FromResult<PlaybackSource?>(null);

            try
            {
                var streamUrl = $"{BaseApiUrl}/tracks/{Uri.EscapeDataString(externalId)}/stream?app_name={AppName}";

                var source = new PlaybackSource
                {
                    Provider = Name,
                    Url = streamUrl,
                    MimeType = "audio/mpeg",
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                return Task.FromResult<PlaybackSource?>(source);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudiusProvider] GetPlaybackSourceAsync error: {ex.Message}");
                return Task.FromResult<PlaybackSource?>(null);
            }
        }

        public async Task<AudioStreamResult?> GetAudioStreamAsync(string externalId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return null;

            try
            {
                var streamUrl = $"{BaseApiUrl}/tracks/{Uri.EscapeDataString(externalId)}/stream?app_name={AppName}";
                var response = await httpClient.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AudiusProvider] Audio stream returned HTTP {response.StatusCode} for {externalId}");
                    return null;
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";

                return new AudioStreamResult
                {
                    Stream = stream,
                    MimeType = mediaType
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudiusProvider] GetAudioStreamAsync error for {externalId}: {ex.Message}");
                return null;
            }
        }
    }
}
