using MassTransit;
using MassTransit.RetryPolicies;
using Microsoft.EntityFrameworkCore;

namespace RabbitMQ.Services;

public class MigrationHostedService<IDbContext> : BackgroundService where IDbContext : DbContext
{
	private readonly IServiceScopeFactory scopeFactory;

	public MigrationHostedService(IServiceScopeFactory scopeFactory)
	{
		this.scopeFactory = scopeFactory;
	}

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		await Retry.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15))
			.Retry(async () =>
			{
				await using var scope = scopeFactory.CreateAsyncScope();
				var context = scope.ServiceProvider.GetRequiredService<IDbContext>();
				await context.Database.MigrateAsync(token);
			}, token);
	}
}

