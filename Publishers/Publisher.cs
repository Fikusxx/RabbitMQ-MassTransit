using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace RabbitMQ.Publishers;

public class Publisher : IDisposable
{
	private readonly IConnection connection;
	private readonly IModel channel;

	public Publisher()
	{
		var factory = new ConnectionFactory();
		factory.Uri = new Uri("amqps://xpymutom:TKDT5H__76nWpMIUSRWs6oitzwMYpdRf@chimpanzee.rmq.cloudamqp.com/xpymutom");

		connection = factory.CreateConnection();
		channel = connection.CreateModel();

		//channel.BasicAcks += (sender, args) =>
		//{
		//	Console.WriteLine("Confirmed: " + args.DeliveryTag);
		//};

		//channel.BasicNacks += (sender, args) =>
		//{
		//	Console.WriteLine("Rejected: " + args.DeliveryTag);
		//};

		//channel.ConfirmSelect();

		//channel.ExchangeDeclare(exchange: "Response", type: ExchangeType.Fanout, durable: false, autoDelete: true);
		channel.QueueDeclare(queue: "Response", durable: false, autoDelete: true, exclusive: true);
		//channel.QueueBind(queue: "Response", exchange: "Response", routingKey: "");
	}

	public void Publish()
	{
		var props = channel.CreateBasicProperties();
		props.ReplyTo = "Response";
		props.CorrelationId = "123";

		var testObject = new { Name = "Fikus", Age = new Random().Next(1, 100) };
		var message = JsonSerializer.Serialize(testObject);
		var body = Encoding.UTF8.GetBytes(message);

		if (connection.IsOpen)
			channel.BasicPublish(exchange: "in", routingKey: "in", basicProperties: props, body: body);

		var consumer = new EventingBasicConsumer(channel);

		consumer.Received += (module, args) =>
		{
			var body = args.Body;
			var message = Encoding.UTF8.GetString(body.ToArray());

			if (args.BasicProperties.CorrelationId == "123")
				Console.WriteLine(message);
		};

		channel.BasicConsume(queue: "Response", autoAck: false, consumer: consumer);
	}

	public void Dispose()
	{
		if (channel.IsOpen)
		{
			channel.Close();
			connection.Close();
		}
	}
}
