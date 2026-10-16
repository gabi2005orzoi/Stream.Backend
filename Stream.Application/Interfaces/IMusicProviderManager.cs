using Stream.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
