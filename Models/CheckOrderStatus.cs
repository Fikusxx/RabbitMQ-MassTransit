namespace RabbitMQ.Models;

public record CheckOrderStatus
{
	public string OrderId { get; init; }
}

public record OrderStatusResult
{
	public string OrderId { get; init; }
	public DateTime Timestamp { get; init; }
	public short StatusCode { get; init; }
	public string StatusText { get; init; }
}