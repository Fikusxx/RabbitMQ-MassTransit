using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace RabbitMQ.Hubs;

public class AuthHubFilter : IHubFilter
{
	public async ValueTask<object> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
	{
		//throw new AuthException("Expired");

		var expiry = context.Context.User!.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)!.Value;
		var expiryDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry));

		if (DateTimeOffset.UtcNow.Subtract(expiryDate) > TimeSpan.Zero)
		{
			throw new AuthException("Expired");
		}

		return await next(context);
	}

	public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next) 
		=> next(context);

	public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next) 
		=> next(context, exception);
}

public class AuthException : HubException
{
    public AuthException(string message) : base(message)
    {
        
    }
}
