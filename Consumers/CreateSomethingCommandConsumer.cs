using MassTransit;
using RabbitMQ.Models;

namespace RabbitMQ.Consumers;

public class CreateSomethingCommandConsumer : IConsumer<CreateSomethingCommand>
{
	public Task Consume(ConsumeContext<CreateSomethingCommand> context)
	{
        var message = context.Message;
        Console.WriteLine("Privet :)");

        return Task.CompletedTask;
    }
}
