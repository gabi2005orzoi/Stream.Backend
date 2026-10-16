using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stream.Application.DTOs.ListeningHistory;
using Stream.Application.Interfaces;

namespace Stream.Api.Controllers
{
    [Authorize]
    [Route("api/history")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService historyService;

        public HistoryController(IHistoryService historyService)
        {
            this.historyService = historyService;
        }

        [HttpPost]
        public async Task<IActionResult> RecordPlay(
            [FromBody] RecordHistoryDto dto,
            CancellationToken cancellationToken)
        {
            await historyService.RecordPlayAsync(dto, cancellationToken);
            return Ok(new { message = "Track added to history list" });
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
        {
            if (page.HasValue || pageSize.HasValue)
            {
                var paged = await historyService.GetPagedHistoryAsync(page ?? 1, pageSize ?? 10, cancellationToken);
                return Ok(paged);
            }

            var history = await historyService.GetRecentHistoryAsync(limit, cancellationToken);
            return Ok(history);
        }

        [HttpDelete]
        public async Task<IActionResult> Clear(CancellationToken cancellationToken)
        {
            await historyService.ClearHistoryAsync(cancellationToken);
            return Ok(new { message = "The history was deleted" });
        }
    }
}
