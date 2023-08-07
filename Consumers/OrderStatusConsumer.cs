using MassTransit;
using RabbitMQ.ErrorModels;
using RabbitMQ.Models;

namespace RabbitMQ.Consumers;

public class OrderStatusConsumer : IConsumer<CheckOrderStatus>
{
	public async Task Consume(ConsumeContext<CheckOrderStatus> context)
	{
		var result = new OrderStatusResult()
		{
			OrderId = "Privet",
			StatusCode = 1,
			StatusText = "OK",
			Timestamp = DateTime.Now
		};

		await context.RespondAsync(result);
	}
}

public class OrderStatusConsumerDefinition : ConsumerDefinition<OrderStatusConsumer>
{
	protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<OrderStatusConsumer> consumerConfigurator)
	{
		endpointConfigurator.DiscardFaultedMessages();
	}
}