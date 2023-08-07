using MassTransit;

namespace RabbitMQ.Services;

public interface IEventBus
{
	public Task PublishAsync<T>(T message) where T : class;
}

public class EventBus : IEventBus
{
	private readonly IPublishEndpoint publishEndpoint;

	public EventBus(IPublishEndpoint publishEndpoint)
	{
		this.publishEndpoint = publishEndpoint;
	}

	public async Task PublishAsync<T>(T message) where T : class
	{
		await publishEndpoint.Publish(message);
	}
}
