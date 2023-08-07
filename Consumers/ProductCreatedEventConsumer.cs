using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Models;
using RabbitMQ.Services;
using System.Data;
using System.Diagnostics;

namespace RabbitMQ.Consumers;

public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
{
	public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
	{
        await Console.Out.WriteLineAsync("Recieved #1");
        var data = context.Message;
		Console.WriteLine($"Consumer #1: {data.Name} | {data.Age}");
		var isType = context.HasMessageType(typeof(ProductCreatedEvent));
		var types = context.SupportedMessageTypes;
		var id = context.MessageId;

		await Task.CompletedTask;
	}
}

//public class ProductCreatedEventConsumerDefinition : ConsumerDefinition<ProductCreatedEventConsumer>
//{
//	private readonly IServiceProvider serviceProvider;
//	public ProductCreatedEventConsumerDefinition(IServiceProvider serviceProvider)
//	{
//		//EndpointName = "product-created-event";
//		ConcurrentMessageLimit = 10;
//		this.serviceProvider = serviceProvider;
//	}

//	protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
//		IConsumerConfigurator<ProductCreatedEventConsumer> consumerConfigurator)
//	{
//		consumerConfigurator.UseMessageRetry(retry =>
//		{
//			retry.Interval(2, 1000);
//			retry.Ignore<ArgumentNullException>();
//			retry.Handle<DataException>();
//		});

//		consumerConfigurator.ConcurrentMessageLimit = 10;
//		// endpoint code..
//		endpointConfigurator.PrefetchCount = 100;
//		endpointConfigurator.ConcurrentMessageLimit = 10;
//		endpointConfigurator.UseEntityFrameworkOutbox<DbContext>(serviceProvider);
//	}
//}

public class ProductCreatedEventConsumerTwo : IConsumer<Batch<ProductCreatedEvent>>
{
	public async Task Consume(ConsumeContext<Batch<ProductCreatedEvent>> context)
	{
		for (var i = 0; i < context.Message.Length; i++)
		{
            await Console.Out.WriteLineAsync("Recieved #2");
            ConsumeContext<ProductCreatedEvent> message = context.Message[i];
			ProductCreatedEvent data = message.Message;
			Console.WriteLine($"Consumer #2: {data.Name} | {data.Age}");
		}

		await Task.CompletedTask;
	}
}