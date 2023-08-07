using MassTransit;
using RabbitMQ.Models;
using RabbitMQ.SagaStateMachine;

namespace RabbitMQ.Test;

public record ProcessOrderRequest(Guid OrderId);

public record OrderProcessedResponse(Guid OrderId);

public class ProcessOrderConsumer : IConsumer<ProcessOrderRequest>
{
	public async Task Consume(ConsumeContext<ProcessOrderRequest> context)
	{
		await context.RespondAsync(new OrderProcessedResponse(context.Message.OrderId));
	}
}

public class OrderStateTwo : SagaStateMachineInstance
{
	public Guid CorrelationId { get; set; }
	public string CurrentState { get; set; }
	public Guid OrderId { get; set; }
}

public class OrderStateMachineTwo : MassTransitStateMachine<OrderStateTwo>
{
	public State Created { get; set; }

	public State Cancelled { get; set; }

	public Event<TestClass> OrderSubmitted { get; set; }

	public Request<OrderStateTwo, ProcessOrderRequest, OrderProcessedResponse> ProcessOrder { get; set; }

	public OrderStateMachineTwo()
	{
		InstanceState(m => m.CurrentState);

		Request(() => ProcessOrder, config => { config.Timeout = TimeSpan.Zero; });

		Initially(
			When(OrderSubmitted)
				.Request(ProcessOrder, context => new ProcessOrderRequest(context.Saga.CorrelationId))
				.TransitionTo(ProcessOrder.Pending));

		During(ProcessOrder.Pending,
			When(ProcessOrder.Completed)
				.Publish(context => new TestEvent() { CurrentState = "Payment Completed" })
				.TransitionTo(Created),
			When(ProcessOrder.Faulted)
				.TransitionTo(Cancelled),
			When(ProcessOrder.TimeoutExpired)
				.TransitionTo(Cancelled));
	}
}

public class TestClass
{
	public Guid OrderId { get; set; }
	public DateTime OrderDate { get; set; }
	public Guid CorrelationId => OrderId;
}