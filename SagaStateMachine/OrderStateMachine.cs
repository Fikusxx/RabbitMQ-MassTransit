using MassTransit;
using RabbitMQ.Models;

namespace RabbitMQ.SagaStateMachine;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
	public State Submitted { get; private set; }
	public State Accepted { get; private set; }
	public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; }
	public Event<OrderAcceptedEvent> OrderAccepted { get; private set; }

	public OrderStateMachine()
	{
		InstanceState(x => x.CurrentState);

		Initially(
			When(OrderSubmitted)
				.Publish(context => new TestEvent() { CurrentState = context.Saga.CurrentState, OrderId = context.Saga.CorrelationId })
				.Then(x => x.Saga.OrderDate = x.Message.OrderDate)
				.Send(context => new ProductCreatedEvent() { Name = "Fikus", Age = 777 })
				.TransitionTo(Submitted)
				.Publish(context => new TestEvent() { CurrentState = context.Saga.CurrentState, OrderId = context.Saga.CorrelationId }));
		//When(OrderAccepted)
		//	.TransitionTo(Accepted));

		During(Submitted,
			When(OrderAccepted)
				.TransitionTo(Accepted)
				.Publish(context => new TestEvent() { CurrentState = context.Saga.CurrentState, OrderId = context.Saga.CorrelationId })
				.Finalize()
				.Publish(context => new TestEvent() { CurrentState = context.Saga.CurrentState, OrderId = context.Saga.CorrelationId }));

		//During(Accepted,
		//	When(OrderSubmitted)
		//		.Then(x => x.Saga.OrderDate = x.Message.OrderDate)
		//		.Finalize());

		SetCompletedWhenFinalized();
	}
}

public class OrderState : SagaStateMachineInstance
{
	public Guid CorrelationId { get; set; }
	public string CurrentState { get; set; }
	public DateTime? OrderDate { get; set; }
}

public class OrderSubmittedEvent
{
	public Guid OrderId { get; set; }
	public DateTime OrderDate { get; set; }
	public Guid CorrelationId => OrderId;
}

public class OrderAcceptedEvent /*: CorrelatedBy<Guid>*/
{
	public Guid OrderId { get; set; }
	public Guid CorrelationId => OrderId;
}