using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Connecction;
using System.Text;
using System.Text.Json;

namespace RabbitMQ.Consumers;

public class MotherConsumer : BackgroundService
{
	private readonly RabbitConnection rabbitConnection;
	private readonly IModel channel;
	private List<RegularConsumer> consumers = new();
	private readonly string rabbitQueue = "Rabbit";
	private readonly string failedQueue = "Failed";

	public MotherConsumer(RabbitConnection rabbitConnection)
	{
		this.rabbitConnection = rabbitConnection;
		channel = rabbitConnection.CreateChannel();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var consumer = new RegularConsumer(rabbitConnection.CreateChannel(), rabbitQueue);
		consumer.Init();

		consumers.Add(consumer);

		while (true)
		{
			await Task.Delay(1000);

			if (channel.MessageCount(failedQueue) > 5 && consumers.Count == 1)
			{
				var failedConsumer = new RegularConsumer(rabbitConnection.CreateChannel(), failedQueue);
				failedConsumer.Init();
				consumers.Add(failedConsumer);
			}

			if (channel.MessageCount(failedQueue) == 0 && consumers.Count > 1)
			{
				var c = consumers.LastOrDefault();
				c.Dispose();
			}
		}

		await Task.CompletedTask;
	}

	public override void Dispose()
	{
		if (channel.IsOpen)
		{
			consumers.ForEach(x => x.Dispose());
			channel.Close();
		}

		base.Dispose();
	}
}

public class RegularConsumer : IDisposable
{
	private readonly IModel channel;
	private readonly string queueName;

	public RegularConsumer(IModel model, string queueName)
	{
		this.channel = model;
		this.queueName = queueName;
	}

	public void Init()
	{
		channel.BasicQos(0, 1, false);

		var consumer = new EventingBasicConsumer(channel);

		consumer.Received += (module, args) =>
		{
			var body = args.Body;
			var message = Encoding.UTF8.GetString(body.ToArray());
			var person = JsonSerializer.Deserialize<Person>(message);
			Console.WriteLine($"Consumer: {person.Name} | {person.Age}");

			//if (new Random().Next(1, 10) >= 5)
			//{
			//	channel.BasicAck(args.DeliveryTag, false); // if everything went okay
			//}
			//else
			//{
			channel.BasicReject(args.DeliveryTag, false); // if there was some error
														  //}

			var replyTo = args.BasicProperties.ReplyTo;
			var corrId = args.BasicProperties.CorrelationId;

			//channel.BasicPublish(exchange: replyTo, routingKey: "", basicProperties: args.BasicProperties, Encoding.UTF8.GetBytes("Responded!"));
			channel.BasicPublish(exchange: "", routingKey: replyTo, basicProperties: args.BasicProperties, Encoding.UTF8.GetBytes("Responded!"));
		};

		channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
	}

	public void Dispose()
	{
		if (channel.IsOpen)
			channel.Close();
	}
}
