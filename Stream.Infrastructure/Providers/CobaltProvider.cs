using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Stream.Infrastructure.Providers
{
    public class CobaltProvider : IMusicProvider
    {
        public string Name => "Cobalt";

        private readonly HttpClient httpClient;
        private readonly List<string> instances;
        private readonly string? apiKey;

        public CobaltProvider(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;

            var configuredUrl = configuration["COBALT_API_URL"] 
                ?? configuration["Cobalt:ApiUrl"] 
                ?? Environment.GetEnvironmentVariable("COBALT_API_URL");

            this.apiKey = configuration["COBALT_API_KEY"] 
                ?? configuration["Cobalt:ApiKey"] 
                ?? Environment.GetEnvironmentVariable("COBALT_API_KEY");

            instances = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                instances.Add(configuredUrl.TrimEnd('/'));
            }

            var defaultInstances = new[]
            {
                "https://api.cobalt.tools",
                "https://cobalt-api.kwiatekm.tokyo"
            };

            foreach (var d in defaultInstances)
            {
                if (!instances.Contains(d, StringComparer.OrdinalIgnoreCase))
                {
                    instances.Add(d);
                }
            }
        }

        public Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            // Cobalt is an audio/video extraction service, not a search index.
            return Task.FromResult<IReadOnlyList<TrackCandidate>>([]);
        }

        public async Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return null;

            var targetUrl = externalId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? externalId
                : $"https://www.youtube.com/watch?v={externalId}";

            var payload = new
            {
                url = targetUrl,
                downloadMode = "audio",
                audioFormat = "mp3"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            foreach (var instance in instances)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, $"{instance}/")
                    {
                        Content = jsonContent
                    };

                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        request.Headers.TryAddWithoutValidation("Api-Key", apiKey);
                    }

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(7));

                    var response = await httpClient.SendAsync(request, cts.Token);
                    if (!response.IsSuccessStatusCode)
                        continue;

                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    string? streamUrl = null;
                    if (root.TryGetProperty("url", out var urlProp))
                    {
                        streamUrl = urlProp.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(streamUrl))
                    {
                        return new PlaybackSource
                        {
                            Provider = Name,
                            Url = streamUrl,
                            MimeType = "audio/mpeg",
                            ExpiresAt = DateTime.UtcNow.AddHours(4)
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CobaltProvider] Extraction failed on {instance} for {externalId}: {ex.Message}");
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
                Console.WriteLine($"[CobaltProvider] GetAudioStreamAsync failed for {externalId}: {ex.Message}");
                return null;
            }
        }
    }
}
