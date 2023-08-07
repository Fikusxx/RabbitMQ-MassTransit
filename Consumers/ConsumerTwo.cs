using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Connecction;
using System.Text;
using System.Text.Json;

namespace RabbitMQ.Consumers;

public class ConsumerTwo : BackgroundService
{
	//private readonly IConnection connection;
	private readonly IModel channel;
	private readonly RabbitConnection rabbitConnection;

	public ConsumerTwo(RabbitConnection rabbitConnection)
	{
		this.rabbitConnection = rabbitConnection;
		//var factory = new ConnectionFactory();
		//factory.Uri = new Uri("amqps://xpymutom:TKDT5H__76nWpMIUSRWs6oitzwMYpdRf@chimpanzee.rmq.cloudamqp.com/xpymutom");

		//connection = factory.CreateConnection();
		//channel = connection.CreateModel();
		channel = rabbitConnection.CreateChannel();
		channel.BasicQos(0, 1, false);
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var consumer = new EventingBasicConsumer(channel);

		Person? person = new();

		consumer.Received += (module, args) =>
		{
			var body = args.Body;
			var message = Encoding.UTF8.GetString(body.ToArray());
			person = JsonSerializer.Deserialize<Person>(message);
			Console.WriteLine($"Consumer #2: {person.Name} | {person.Age}");

			// service calls..

			if (new Random().Next(1, 10) >= 5)
			{
				channel.BasicAck(args.DeliveryTag, false); // if everything went okay
			}
			else
			{
				channel.BasicReject(args.DeliveryTag, false); // if there was some error
			}
		};

		channel.BasicConsume(queue: "Rabbit", autoAck: false, consumer: consumer);

		return Task.CompletedTask;
	}

	//public override void Dispose()
	//{
	//	if (channel.IsOpen)
	//	{
	//		channel.Close();
	//		connection.Close();
	//	}

	//	base.Dispose();
	//}
}

