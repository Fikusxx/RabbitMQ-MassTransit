using Microsoft.AspNetCore.Mvc;

namespace RabbitMQ.Controllers.v2;

[ApiController]
//[Route("[controller]")]
[Route("api/{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[ApiVersion("2.1")]
public class VersionController : ControllerBase
{
	[HttpGet]
	[MapToApiVersion("2.0")]
	public IActionResult Get()
	{
		return Ok("version 2");
	}

	[HttpGet]
	[MapToApiVersion("2.1")]
	public IActionResult GetNew()
	{
		return Ok("version 2.1");
	}
}
