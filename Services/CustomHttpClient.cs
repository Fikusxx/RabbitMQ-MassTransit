using RabbitMQ.Models;
using Polly;

namespace RabbitMQ.Services;

public class MyHttpClient : BaseHttpClient<ProductCreatedEvent>
{
	private int count = 0;

	public MyHttpClient(HttpClient client) : base(client)
	{ }

	public async Task<ProductCreatedEvent> Execute()
	{
		var result = await policyWrap.ExecuteAndCaptureAsync(async () =>
		{
			count++;

			if (count == 4)
			{
				await Console.Out.WriteLineAsync("Executed");
				return new ProductCreatedEvent() { Name = "Fikus", Age = 123 };
			}

			throw new Exception();
		});

		return result.Result;
	}
}

public abstract class BaseHttpClient<T> where T : class
{
	protected readonly HttpClient httpClient;
	protected IAsyncPolicy<T> policyWrap = null!;
	private IAsyncPolicy<T> retryPolicy = null!;
	private IAsyncPolicy curcuitPolicy = null!;

	public BaseHttpClient(HttpClient httpClient)
	{
		this.httpClient = httpClient;
		InitPolicies();
	}

	private void InitPolicies()
	{
		retryPolicy = Policy<T>
			.Handle<Exception>()
			.WaitAndRetryAsync(
			retryCount: 5,
			retryAttempt =>
			{
				return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)); // retryAttempt = 1,2,3,4,5
			},
			onRetry: (exception, waitTime, ctx) =>
			{
				Console.WriteLine($"Base Policy Exception: " + exception.Exception.GetType());
				Console.WriteLine($"Base Policy Wait time: " + waitTime); // 0:02 | 0:04 | 0:08 | 0:16 | ...
			});

		curcuitPolicy = Policy
			.Handle<Exception>()
			.CircuitBreakerAsync(
				exceptionsAllowedBeforeBreaking: 3,
				durationOfBreak: TimeSpan.FromSeconds(10),
				onBreak: (exception, waitTime) =>
				{
					Console.WriteLine("Base Policy Break: " + exception.GetType() + " || " + "Waittime: " + waitTime);
					// action on break
				},
				onReset: () =>
				{
					Console.WriteLine("Base Policy Reset");
					// action on reset
				},
				onHalfOpen: () =>
				{
                    Console.WriteLine("Base Policy Half Open");
					// action on half open
                });

		policyWrap = retryPolicy.WrapAsync(curcuitPolicy);
	}
}


public class CustomHttpClient
{
	private readonly HttpClient httpClient;

	public CustomHttpClient(IHttpClientFactory factory)
	{
		httpClient = factory.CreateClient("namedClient");
	}
}

public class SomeService
{
	private readonly HttpClient client;

	public SomeService(HttpClient client)
	{
		this.client = client;
	}

	public async Task<CheckOrderStatus> GetOrderAsync(string username)
	{
		var content = await client.GetFromJsonAsync<CheckOrderStatus>(username);

		return content;
	}
}
