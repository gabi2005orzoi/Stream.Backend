using Microsoft.AspNetCore.Mvc;

namespace Stream.Api.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [HttpHead]
        public IActionResult Health()
        {
            return Ok();
        }
    }
}
