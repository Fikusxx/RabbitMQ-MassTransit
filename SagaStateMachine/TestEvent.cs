using Microsoft.EntityFrameworkCore;

namespace RabbitMQ.SagaStateMachine;

public class TestEvent
{
	public Guid OrderId { get; set; }	
	public string CurrentState { get; set; }
}