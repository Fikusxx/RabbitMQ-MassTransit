using MassTransit.Configuration;
using MassTransit;
using RabbitMQ.Services;

namespace RabbitMQ.Middlewares;

public static class MessageFilterConfigurationExtensions
{
	public static void UseMessageFilter(this IConsumePipeConfigurator configurator)
	{
		if (configurator == null)
			throw new ArgumentNullException(nameof(configurator));

		var observer = new MessageFilterConfigurationObserver(configurator);
	}
}

public class MessageFilterConfigurationObserver : ConfigurationObserver, IMessageConfigurationObserver
{
	public MessageFilterConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator)
		: base(receiveEndpointConfigurator)
	{
		Connect(this);
	}

	public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
		where TMessage : class
	{
		var specification = new MessageFilterPipeSpecification<TMessage>();

		configurator.AddPipeSpecification(specification);
	}
}

public class MessageFilterPipeSpecification<T> : IPipeSpecification<ConsumeContext<T>> where T : class
{
	public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
	{
		var filter = new MessageFilter<T>();

		builder.AddFilter(filter);
	}

	public IEnumerable<ValidationResult> Validate()
	{
		yield break;
	}
}

public class MessageFilter<T> : IFilter<ConsumeContext<T>> where T : class
{

	public void Probe(ProbeContext context)
	{
		var scope = context.CreateFilterScope("messageFilter");
	}

	public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
	{
		await Console.Out.WriteLineAsync($"MessageType Filter: Pre | Type: {typeof(T).Name}");

		await next.Send(context);

		await Console.Out.WriteLineAsync($"MessageType Filter: Post | Type: {typeof(T).Name}");
	}
}