using Stream.Application.DTOs;
using Stream.Domain.Models;

namespace Stream.Application.Interfaces
{
    public interface ITrackResolver
    {
        Task<Track> ResolveAsync(
            TrackCandidate candidate,
            CancellationToken cancellationToken);
        Task<IReadOnlyList<Track>> ResolveBatchAsync(IReadOnlyList<TrackCandidate> candidates, CancellationToken cancellationToken);
    }
}
