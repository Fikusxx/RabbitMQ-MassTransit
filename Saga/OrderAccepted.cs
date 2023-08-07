using MassTransit;

namespace RabbitMQ.Saga;

public class OrderAccepted : CorrelatedBy<Guid>
{
	public Guid CorrelationId { get; set; }
	public DateTime Timestamp { get; set; }
}
