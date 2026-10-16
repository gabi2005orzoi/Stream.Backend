using Microsoft.AspNetCore.Mvc;
using Stream.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Stream.Api.Controllers
{
    [Route("api/recommendations")]
    [ApiController]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService recommendationService;

        public RecommendationsController(IRecommendationService recommendationService)
        {
            this.recommendationService = recommendationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations(
            [FromQuery] int limit = 10,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var result = await recommendationService.GetRecommendationsAsync(limit, cancellationToken);
            return Ok(result);
        }
    }
}
