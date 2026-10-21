using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Stream.Infrastructure.Providers
{
    public class YoutubeProvider : IMusicProvider
    {
        public string Name => "Youtube";

        private readonly YoutubeClient youtube;
        private readonly InvidiousProvider invidiousProvider;
        private readonly CobaltProvider cobaltProvider;

        public YoutubeProvider(
            YoutubeClient youtube, 
            InvidiousProvider invidiousProvider, 
            CobaltProvider cobaltProvider)
        {
            this.youtube = youtube;
            this.invidiousProvider = invidiousProvider;
            this.cobaltProvider = cobaltProvider;
        }

        public async Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var results = new List<TrackCandidate>();

            try
            {
                await foreach(var video in youtube.Search.GetVideosAsync(query, cancellationToken))
                {
                    string artist = video.Author.ChannelTitle;
                    string title = video.Title;

                    string[] separators = new[] { " - ", " – ", " — ", " : " };
                    foreach (var sep in separators)
                    {
                        if (video.Title.Contains(sep))
                        {
                            var parts = video.Title.Split(sep, 2, StringSplitOptions.None);
                            if (!string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                            {
                                artist = parts[0].Trim();
                                title = parts[1].Trim();
                                break;
                            }
                        }
                    }

                    results.Add(new TrackCandidate
                    {
                        Provider = Name,
                        ExternalId = video.Id.Value,
                        Title = title,
                        Artist = artist,
                        Album = null,
                        Duration = video.Duration,
                        ArtworkUrl = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url,
                        ExternalUrl = video.Url
                    });

                    if (results.Count >= 20)
                        break;
                }

                if (results.Count > 0)
                    return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YoutubeProvider] YoutubeExplode search error: {ex.Message}. Falling back to Invidious search...");
            }

            // Fallback to Invidious search if YouTube search failed on server
            try
            {
                var invResults = await invidiousProvider.SearchAsync(query, cancellationToken);
                return invResults.Select(r => new TrackCandidate
                {
                    Provider = Name,
                    ExternalId = r.ExternalId,
                    Title = r.Title,
                    Artist = r.Artist,
                    Album = r.Album,
                    Duration = r.Duration,
                    ArtworkUrl = r.ArtworkUrl,
                    ExternalUrl = r.ExternalUrl
                }).ToList();
            }
            catch
            {
                return [];
            }
        }

        public async Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, CancellationToken cancellationToken)
        {
            try
            {
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(externalId, cancellationToken);

                var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();

                if(audioStreamInfo == null)
                    throw new InvalidOperationException("No audio streams found in YoutubeExplode manifest.");

                return new PlaybackSource
                {
                    Provider = Name,
                    Url = audioStreamInfo.Url,
                    MimeType = audioStreamInfo.Container.Name switch
                    {
                        "mp4" => "audio/mp4",
                        "webm" => "audio/webm",
                        _ => "audio/mpeg"
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YoutubeProvider] Direct YouTube stream failed on server ({ex.Message}). Trying Invidious & Cobalt fallback...");

                // 1. Invidious fallback for YouTube video ID
                try
                {
                    var invidiousSource = await invidiousProvider.GetPlaybackSourceAsync(externalId, cancellationToken);
                    if (invidiousSource != null)
                    {
                        invidiousSource.Provider = Name;
                        return invidiousSource;
                    }
                }
                catch (Exception invEx)
                {
                    Console.WriteLine($"[YoutubeProvider] Invidious fallback failed for {externalId}: {invEx.Message}");
                }

                // 2. Cobalt fallback for YouTube video ID
                try
                {
                    var cobaltSource = await cobaltProvider.GetPlaybackSourceAsync(externalId, cancellationToken);
                    if (cobaltSource != null)
                    {
                        cobaltSource.Provider = Name;
                        return cobaltSource;
                    }
                }
                catch (Exception cobEx)
                {
                    Console.WriteLine($"[YoutubeProvider] Cobalt fallback failed for {externalId}: {cobEx.Message}");
                }

                return null;
            }
        }

        public async Task<AudioStreamResult?> GetAudioStreamAsync(string externalId, CancellationToken cancellationToken)
        {
            try
            {
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(externalId, cancellationToken);
                var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();

                if (audioStreamInfo == null)
                    throw new InvalidOperationException("No audio streams found in YoutubeExplode manifest.");

                var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo, cancellationToken);

                return new AudioStreamResult
                {
                    Stream = stream,
                    MimeType = audioStreamInfo.Container.Name switch
                    {
                        "mp4" => "audio/mp4",
                        "webm" => "audio/webm",
                        _ => "audio/mpeg"
                    }
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[YoutubeProvider] Direct YouTube audio stream failed ({ex.Message}). Trying Invidious & Cobalt proxy fallback...");

                // 1. Try Invidious
                try
                {
                    var invAudio = await invidiousProvider.GetAudioStreamAsync(externalId, cancellationToken);
                    if (invAudio != null)
                        return invAudio;
                }
                catch (Exception invEx)
                {
                    Console.WriteLine($"[YoutubeProvider] Invidious audio stream failed for {externalId}: {invEx.Message}");
                }

                // 2. Try Cobalt
                try
                {
                    var cobAudio = await cobaltProvider.GetAudioStreamAsync(externalId, cancellationToken);
                    if (cobAudio != null)
                        return cobAudio;
                }
                catch (Exception cobEx)
                {
                    Console.WriteLine($"[YoutubeProvider] Cobalt audio stream failed for {externalId}: {cobEx.Message}");
                }

                return null;
            }
        }
    }
}
