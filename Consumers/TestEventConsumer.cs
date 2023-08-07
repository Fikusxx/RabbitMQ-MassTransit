using MassTransit;
using RabbitMQ.SagaStateMachine;

namespace RabbitMQ.Consumers;

public class TestEventConsumer : IConsumer<TestEvent>
{
	public Task Consume(ConsumeContext<TestEvent> context)
	{
		var message = context.Message;
        Console.WriteLine($"OrderId: {message.OrderId} was {message.CurrentState}");
        return Task.CompletedTask;
	}
}
