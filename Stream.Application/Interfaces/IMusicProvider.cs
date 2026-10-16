using Stream.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IMusicProvider
    {
        string Name { get; }
        Task<IReadOnlyList<TrackCandidate>> SearchAsync(
            string query,
            CancellationToken cancellationToken = default);

        Task<PlaybackSource?> GetPlaybackSourceAsync(
            string externalId,
            CancellationToken cancellationToken);

        Task<AudioStreamResult?> GetAudioStreamAsync(
            string externalId,
            CancellationToken cancellationToken = default);
    }
}
