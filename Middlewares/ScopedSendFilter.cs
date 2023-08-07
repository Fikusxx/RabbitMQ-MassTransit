using MassTransit;

namespace RabbitMQ.Middlewares;

public class MySendFilter<T> : IFilter<SendContext<T>> where T : class
{
	public async Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
	{
		await Console.Out.WriteLineAsync($"Send Filter: Pre | Type: {typeof(T).Name}");

		await next.Send(context);

		await Console.Out.WriteLineAsync($"Send Filter: Post | Type: {typeof(T).Name}");
	}

	public void Probe(ProbeContext context) 
	{
		var scope = context.CreateFilterScope("sendFilter");
	}
}
