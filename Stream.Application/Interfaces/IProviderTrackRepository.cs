using Stream.Domain.Models;

namespace Stream.Application.Interfaces
{
    public interface IProviderTrackRepository
    {
        Task<ProviderTrack?> FindAsync(
            Guid trackId,
            string provider,
            string externalId,
            CancellationToken cancellationToken);

        Task AddAsync(ProviderTrack track, CancellationToken cancellationToken);

        Task<ProviderTrack?> FindByProviderAndExternalIdAsync(
            string provider,
            string externalId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ProviderTrack>> FindByExternalIdsAsync(string provider, IEnumerable<string> externalIds, CancellationToken cancellation);

        Task AddRangeAsync(IEnumerable<ProviderTrack> tracks, CancellationToken cancellationToken);
    }
}
