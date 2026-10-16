using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stream.Application.Interfaces;

namespace Stream.Api.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService searchService;

        public SearchController(ISearchService searchService)
        {
            this.searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (page.HasValue || pageSize.HasValue)
            {
                var pagedResults = await searchService.SearchPagedAsync(
                    q, page ?? 1, pageSize ?? 10, cancellationToken);
                return Ok(pagedResults);
            }

            var results = await searchService.SearchAsync(q, cancellationToken);
            return Ok(results);
        }
    }
}
