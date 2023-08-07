using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RabbitMQ.Controllers.v1;

[ApiController]
//[Route("[controller]")]
[Route("api/{version:apiVersion}/[controller]")]
[ApiVersion("1.0", Deprecated = true)]
[EnableRateLimiting("fixedIP")]
public class VersionController : ControllerBase
{
	[HttpGet]
	public IActionResult Get()
	{
		return Ok("version 1");
	}
}
