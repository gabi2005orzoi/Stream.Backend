using Stream.Application.DTOs;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stream.Application.Interfaces
{
    public interface IMusicProviderManager
    {
        Task<IReadOnlyList<TrackCandidate>> SearchAsync(
            string query,
            CancellationToken cancellationToken);

        Task<PlaybackSource?> GetPlaybackSourceAsync(
            string externalId,
            string? providerName = null,
            CancellationToken cancellationToken = default);

        Task<AudioStreamResult?> GetAudioStreamAsync(
            string externalId,
            string? providerName = null,
            CancellationToken cancellationToken = default);

        Task<PlaybackSource?> GetPlaybackSourceForTrackAsync(
            Track track,
            CancellationToken cancellationToken = default);

        Task<AudioStreamResult?> GetAudioStreamForTrackAsync(
            Track track,
            CancellationToken cancellationToken = default);
    }
}
