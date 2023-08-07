using MassTransit;

namespace RabbitMQ.Saga;

public class SubmitOrder : CorrelatedBy<Guid>
{
	public Guid OrderId { get; set; } = Guid.NewGuid();
	public Guid CorrelationId { get => OrderId; }
	public DateTime OrderDate { get; set; }
}
