using MassTransit;

namespace RabbitMQ.Middlewares;

public class MyPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
	public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
	{
		await Console.Out.WriteLineAsync($"Publish Filter: Pre | Type: {typeof(T).Name}");

		await next.Send(context);

		await Console.Out.WriteLineAsync($"Publish Filter: Post | Type: {typeof(T).Name}");
	}

	public void Probe(ProbeContext context) { }
}