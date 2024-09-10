using Microsoft.AspNetCore.Mvc;

namespace RabbitMQ.Controllers.v2;

[ApiController]
[Route("api/{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[ApiVersion("2.1")]
public class VersionController : ControllerBase
{
	[HttpGet]
	[MapToApiVersion("2.0")]
	public IActionResult GetV2()
	{
		return Ok("version 2");
	}

	[HttpGet]
	[MapToApiVersion("2.1")]
	public IActionResult GetV2_1()
	{
		return Ok("version 2.1");
	}
}
