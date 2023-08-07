namespace RabbitMQ.Saga;

public record OrderShipped
{
	public Guid OrderId { get; set; }
	public DateTime ShipDate { get; set; }
}
