using RabbitMQ.Client;
using System.Threading.Channels;

namespace RabbitMQ.Connecction;

public class RabbitConnection
{
	private readonly IConnection connection;

    public RabbitConnection()
    {
		var factory = new ConnectionFactory();
		factory.Uri = new Uri("amqps://xpymutom:TKDT5H__76nWpMIUSRWs6oitzwMYpdRf@chimpanzee.rmq.cloudamqp.com/xpymutom");

		connection = factory.CreateConnection();
	}

	public IModel CreateChannel()
	{
		return connection.CreateModel();
	}
}
