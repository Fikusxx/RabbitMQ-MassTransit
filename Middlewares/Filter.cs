using MassTransit;
using MassTransit.Configuration;

namespace RabbitMQ.Middlewares;

public static class ExampleMiddlewareConfiguratorExtensions
{
	public static void UseExceptionLogger<T>(this IPipeConfigurator<T> configurator)
		where T : class, PipeContext
	{
		configurator.AddPipeSpecification(new ExceptionLoggerSpecification<T>());
	}
}

public class ExceptionLoggerSpecification<T> : IPipeSpecification<T> where T : class, PipeContext
{
	public IEnumerable<ValidationResult> Validate()
	{
		return Enumerable.Empty<ValidationResult>();
	}

	public void Apply(IPipeBuilder<T> builder)
	{
		builder.AddFilter(new ExceptionLoggerFilter<T>());
	}
}

public class ExceptionLoggerFilter<T> : IFilter<T> where T : class, PipeContext
{
	long _exceptionCount;
	long _successCount;
	long _attemptCount;

	public void Probe(ProbeContext context)
	{
		var scope = context.CreateFilterScope("exceptionLogger");
		scope.Add("attempted", _attemptCount);
		scope.Add("succeeded", _successCount);
		scope.Add("faulted", _exceptionCount);
	}

	public async Task Send(T context, IPipe<T> next)
	{
		try
		{
			Interlocked.Increment(ref _attemptCount);
			await Console.Out.WriteLineAsync($"Filter: Pre | Type: {typeof(T)}");

			await next.Send(context);

			await Console.Out.WriteLineAsync($"Filter: Post | Type: {typeof(T)}");

			Interlocked.Increment(ref _successCount);
		}
		catch (Exception ex)
		{
			Interlocked.Increment(ref _exceptionCount);

			await Console.Out.WriteLineAsync($"An exception occurred: {ex.Message}");

			throw;
		}
	}
}
