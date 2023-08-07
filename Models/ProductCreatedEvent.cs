using MassTransit;
using RabbitMQ.SagaStateMachine;

namespace RabbitMQ.Models;

public class ProductCreatedEvent : CorrelatedBy<Guid>
{
	public Guid OrderId { get; set; }
	public string Name { get; set; }
	public int Age { get; set; }
	public Guid CorrelationId => OrderId;
}
