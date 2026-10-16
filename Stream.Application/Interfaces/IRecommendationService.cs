using Stream.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Stream.Application.Interfaces
{
    public interface IRecommendationService
    {
        Task<RecommendationResultDto> GetRecommendationsAsync(int limit = 10, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TrackDto>> GetRadioTracksAsync(Guid trackId, int limit = 8, CancellationToken cancellationToken = default);
    }
}
