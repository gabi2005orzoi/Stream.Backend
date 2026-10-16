using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Stream.Infrastructure.Providers
{
    public class JamendoProvider : IMusicProvider
    {
        private readonly HttpClient httpClient;
        private const string ClientId = "56d30c41";

        public JamendoProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public string Name => "Jamendo";

        public async Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var results = new List<TrackCandidate>();

            try
            {
                var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={ClientId}&format=json&limit=10&namesearch={Uri.EscapeDataString(query)}&include=musicinfo";
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return [];

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("results", out var trackArray))
                    return [];

                foreach(var item in trackArray.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString() ?? "";
                    var title = item.GetProperty("name").GetString() ?? "Unknown";
                    var artist = item.GetProperty("artist_name").GetString() ?? "Unknown";
                    var album = item.TryGetProperty("album_name", out var alb) ? alb.GetString() : null;
                    var durationSec = item.GetProperty("duration").GetInt32();
                    var artworkUrl = item.TryGetProperty("image", out var img) ? img.GetString() : null;
                    var externalUrl = item.TryGetProperty("shareurl", out var share) ? share.GetString() : null;
                    results.Add(new TrackCandidate
                    {
                        Provider = Name,
                        ExternalId = id,
                        Title = title,
                        Artist = artist,
                        Album = album,
                        Duration = TimeSpan.FromSeconds(durationSec),
                        ArtworkUrl = artworkUrl,
                        ExternalUrl = externalUrl
                    });
                }
            }
            catch
            {

            }

            return results;
        }

        public async Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, CancellationToken cancellationToken)
        {
            try
            {
                // Jamendo oferă stream direct MP3 prin acest URL oficial
                var streamUrl = $"https://prod-1.storage.jamendo.com/?trackid={externalId}&format=mp31&u=0";
                return new PlaybackSource
                {
                    Provider = Name,
                    Url = streamUrl,
                    MimeType = "audio/mpeg",
                    ExpiresAt = DateTime.UtcNow.AddDays(1)
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<AudioStreamResult?> GetAudioStreamAsync(string externalId, CancellationToken cancellationToken)
        {
            try
            {
                var streamUrl = $"https://prod-1.storage.jamendo.com/?trackid={externalId}&format=mp31&u=0";
                var response = await httpClient.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                return new AudioStreamResult
                {
                    Stream = stream,
                    MimeType = "audio/mpeg"
                };
            }
            catch
            {
                return null;
            }

        }
    }
}
