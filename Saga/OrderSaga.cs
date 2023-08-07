using MassTransit;
using System.Linq.Expressions;

namespace RabbitMQ.Saga;

public class OrderSaga : ISaga, InitiatedBy<SubmitOrder>, Orchestrates<OrderAccepted>, 
	Orchestrates<OrderCanceled>, Observes<OrderShipped, OrderSaga>
{
	public Guid CorrelationId { get; set; }
	public DateTime? SubmitDate { get; set; }
	public DateTime? AcceptDate { get; set; }
	public DateTime? ShipDate { get; set; } 

	public Expression<Func<OrderSaga, OrderShipped, bool>> CorrelationExpression =>
	  (saga, message) => saga.CorrelationId == message.OrderId;

	public async Task Consume(ConsumeContext<SubmitOrder> context)
	{
		SubmitDate = context.Message.OrderDate;

		await context.Publish(new OrderAccepted  { Timestamp = DateTime.Now, CorrelationId = CorrelationId });

		await Task.CompletedTask;
	}

	public async Task Consume(ConsumeContext<OrderAccepted> context)
	{
		AcceptDate = context.Message.Timestamp;

		await context.Publish(new OrderCanceled() { CorrelationId = CorrelationId });

		await Task.CompletedTask;
	}

	public Task Consume(ConsumeContext<OrderShipped> context)
	{
		ShipDate = context.Message.ShipDate;
		return Task.CompletedTask;
	}

	public async Task Consume(ConsumeContext<OrderCanceled> context)
	{
		await context.Publish(new OrderShipped() { OrderId = CorrelationId });
		await Task.CompletedTask;
	}
}
