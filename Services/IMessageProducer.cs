using RabbitMQ.Client;

namespace RabbitMQ.Services;

public interface IMessageProducer
{
	public void SendMessage<T>(T message);
}

public class MessageProducer : IMessageProducer, IDisposable
{
	private readonly IConnection connection;
	private readonly IModel channel;

	public MessageProducer()
	{
		var factory = new ConnectionFactory();
		factory.Uri = new Uri("amqps://xpymutom:TKDT5H__76nWpMIUSRWs6oitzwMYpdRf@chimpanzee.rmq.cloudamqp.com/xpymutom");

		connection = factory.CreateConnection();
		channel = connection.CreateModel();

		channel.QueueDeclare(queue: "Booking", durable: false, exclusive: true, autoDelete: true);
	}

	public void SendMessage<T>(T message)
	{
		throw new NotImplementedException();
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
