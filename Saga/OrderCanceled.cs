using MassTransit;

namespace RabbitMQ.Saga;

public class OrderCanceled : CorrelatedBy<Guid>
{
	public Guid CorrelationId { get; set; }
}
