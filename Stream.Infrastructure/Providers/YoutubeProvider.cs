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

        public YoutubeProvider(YoutubeClient youtube)
        {
            this.youtube = youtube;
        }

        public async Task<IReadOnlyList<TrackCandidate>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var results = new List<TrackCandidate>();

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

            return results;


        }

        public async Task<PlaybackSource?> GetPlaybackSourceAsync(string externalId, CancellationToken cancellationToken)
        {
            try
            {
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(externalId, cancellationToken);

                var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();

                if(audioStreamInfo == null)
                    return null;

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
                Console.WriteLine($"[YoutubeProvider] Error getting playback source for {externalId}: {ex.Message}");
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
                    return null;

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
                Console.WriteLine($"[YoutubeProvider] Error fetching stream for {externalId}: {ex.Message}");
                return null;
            }
        }
    }
}
