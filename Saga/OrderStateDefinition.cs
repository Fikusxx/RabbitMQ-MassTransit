using MassTransit;

namespace RabbitMQ.Saga;

public class OrderStateDefinition : SagaDefinition<OrderSaga>
{
	public OrderStateDefinition()
	{
		// specify the message limit at the endpoint level, which influences
		// the endpoint prefetch count, if supported
		Endpoint(e =>
		{
			e.ConcurrentMessageLimit = 16;
		});

	}

	protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<OrderSaga> sagaConfigurator)
	{
		var partition = endpointConfigurator.CreatePartitioner(16);

		sagaConfigurator.Message<SubmitOrder>(x => x.UsePartitioner(partition, m => m.Message.CorrelationId));
		sagaConfigurator.Message<OrderAccepted>(x => x.UsePartitioner(partition, m => m.Message.CorrelationId));
		sagaConfigurator.Message<OrderCanceled>(x => x.UsePartitioner(partition, m => m.Message.CorrelationId));
	}
}
