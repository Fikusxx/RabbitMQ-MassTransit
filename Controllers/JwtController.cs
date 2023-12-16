using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RabbitMQ.Controllers;

[ApiController]
[Route("jwt")]
public class JwtController : ControllerBase
{
	private readonly IAuthorizationService _authorizationService;

	public JwtController(IAuthorizationService authorizationService)
	{
		_authorizationService = authorizationService;
	}

	[HttpGet]
	//[Authorize(Policy = "TestPolicy")]
	//[Authorize(Policy = IdentityData.AdminPolicyName)]
	//[RequiresClaim(IdentityData.AdminClaimName, "true")]
	[Route("jwt")]
	[Authorize]
	public async Task<IActionResult> Jwt()
	{
		Console.WriteLine("Executed");

		var name = User.FindFirstValue(ClaimTypes.Name);
		var name1 = User.Identity!.Name;
		var name2 = HttpContext.User.Identity.Name; // can be accesible with IHttpContextAccessor

		var resource = new MyResource() { OwnerName = "Petya" };


		var result = await _authorizationService.AuthorizeAsync(User, resource, "EditPolicy");

		if (result.Succeeded)
			return Ok();
		else if (User.Identity!.IsAuthenticated)
			return Forbid();
		else
			return Challenge();


		return Ok();
	}

	[HttpGet]
	[Route("get-jwt")]
	public IActionResult GetJwt()
	{
		var username = "Vasya";
		var claims = new List<Claim>()
		{
			new Claim(JwtRegisteredClaimNames.NameId, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Name, username),
			new Claim(IdentityData.AdminClaimName, "true"),
			new Claim(ClaimTypes.Role, "admin")
		};

		var jwt = new JwtSecurityToken(
			issuer: "someIssuer",
			audience: "someAudience",
			claims: claims,
			//expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(15)),
			expires: DateTime.UtcNow.Add(TimeSpan.FromDays(1)),
			signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("PleaseBeSecurePleaseBeSecurePleaseBeSecure")),
			SecurityAlgorithms.HmacSha256));

		return Ok(new JwtSecurityTokenHandler().WriteToken(jwt));
	}
}

public class MyResource
{
	public string OwnerName { get; set; }
}

public class SameOwnerRequirement : IAuthorizationRequirement { }

public class MyResourcceAuthorizationHandler :
	AuthorizationHandler<SameOwnerRequirement, MyResource>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
												   SameOwnerRequirement requirement,
												   MyResource resource)
	{
		if (context.User.Identity?.Name == resource.OwnerName)
		{
			context.Succeed(requirement);
		}
		else
		{
			context.Fail();
		}

		return Task.CompletedTask;
	}
}