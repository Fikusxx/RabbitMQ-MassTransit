using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RabbitMQ.Identity;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresClaimAttribute : Attribute, IAuthorizationFilter
{
	private readonly string claimName;
	private readonly string claimValue;

	public RequiresClaimAttribute(string claimName, string claimValue)
	{
		this.claimName = claimName;
		this.claimValue = claimValue;
	}

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		if(context.HttpContext.User.HasClaim(claimName, claimValue) == false)
		{
			context.Result = new ForbidResult();
		}
	}
}
