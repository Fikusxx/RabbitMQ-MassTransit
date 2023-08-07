using MassTransit;
using MassTransit.Clients;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Polly.Wrap;
using RabbitMQ.ErrorModels;
using RabbitMQ.Hubs;
using RabbitMQ.Identity;
using RabbitMQ.Models;
using RabbitMQ.Publishers;
using RabbitMQ.SagaStateMachine;
using RabbitMQ.Services;
using RabbitMQ.Test;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Transactions;
using static MassTransit.ValidationResultExtensions;

namespace RabbitMQ.Controllers;

[ApiController]
[Route("rabbit")]
public class RabbitController : ControllerBase
{
	//private readonly Publisher publisher;
	private readonly IEventBus eventBus;
	private readonly IBus bus;
	private readonly ISendEndpointProvider sender;
	private readonly IPublishEndpoint publisher;
	private readonly IRequestClient<CheckOrderStatus> requestClient;
	private static readonly Guid guid = Guid.NewGuid();

	private readonly MyHttpClient myHttpClient;
	private readonly IHubContext<ChatHub, IChatClientNotifications> hubContext;

	public RabbitController(IEventBus eventBus, IBus bus, ISendEndpointProvider sender,
		IPublishEndpoint publisher, IRequestClient<CheckOrderStatus> requestClient, 
		MyHttpClient myHttpClient, IHubContext<ChatHub, IChatClientNotifications> hubContext)
	{
		this.eventBus = eventBus;
		this.bus = bus;
		this.sender = sender;
		this.publisher = publisher;
		this.requestClient = requestClient;
		this.myHttpClient = myHttpClient;
		this.hubContext = hubContext;
	}

	[HttpGet]
	[Authorize]
	//[Authorize(Policy = "TestPolicy")]
	[Authorize(Policy = IdentityData.AdminPolicyName)]
	[RequiresClaim(IdentityData.AdminClaimName, "true")]
	[Route("jwt")]
	public IActionResult Jwt()
	{
		var txOptions = new TransactionOptions() { IsolationLevel = IsolationLevel.Serializable };
		using var transaction = new TransactionScope(TransactionScopeOption.Required, txOptions);
		// dbContext operations
		Console.WriteLine("Executed");

		return Ok();
	}

	[HttpGet]
	[Route("get-jwt")]
	public IActionResult GetJwt()
	{
		var username = "User #1";
		var claims = new List<Claim>()
		{
			new Claim(JwtRegisteredClaimNames.NameId, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Name, username),
			new Claim(IdentityData.AdminClaimName, "true"),
			new Claim(ClaimTypes.Role, "admin")
		};

		var jwt = new JwtSecurityToken(
			issuer: "someIssuer",
			audience: "someAudience",
			claims: claims,
			//expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(15)),
			expires: DateTime.UtcNow.Add(TimeSpan.FromDays(1)),
			signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("PleaseBeSecurePleaseBeSecurePleaseBeSecure")),
			SecurityAlgorithms.HmacSha256));

		return Ok(new JwtSecurityTokenHandler().WriteToken(jwt));
	}


	[HttpGet]
	public async Task<IActionResult> Get()
	{
		var productEvent = new ProductCreatedEvent() { Name = "Fikus", Age = 20 };
		await publisher.Publish(productEvent);
		//await eventBus.PublishAsync(productEvent);

		return Ok();
	}

	[HttpPost]
	public async Task<IActionResult> Post()
	{
		var productEvent = new ProductCreatedEvent() { Name = "Fikus", Age = 20, OrderId = guid };

		//var endpoint = await bus.GetSendEndpoint(new Uri("exchange:product-created-event?temporary=true"));
		//var endpoint = await bus.GetSendEndpoint(new Uri("exchange:product-event?type=direct"));

		await bus.Send(productEvent);

		return Ok();
	}

	[HttpGet]
	[Route("order-check")]
	public async Task<IActionResult> CheckOrderStatus()
	{
		var order = new CheckOrderStatus() { OrderId = "KEKW" };

		//var client = mediator.CreateRequestClient<CheckOrderStatus>();
		//var response = await client.GetResponse<OrderStatusResult>(order);

		var result = await requestClient.GetResponse<OrderStatusResult>(order);

		return Ok();
	}

	[HttpGet]
	[Route("submit-order")]
	public async Task<IActionResult> SubmitOrder()
	{
		var order = new OrderSubmittedEvent() { OrderId = guid, OrderDate = DateTime.Now };
		await publisher.Publish(order);

		return Ok();
	}

	[HttpGet]
	[Route("order-accepted")]
	public async Task<IActionResult> OrderAccepted()
	{
		var order = new OrderAcceptedEvent() { OrderId = guid };
		await publisher.Publish(order);

		return Ok();
	}

	[HttpGet("buy")]
	public async Task<IActionResult> BuyAsync()
	{
		var order = new TestClass() { OrderId = guid, OrderDate = DateTime.Now };
		await publisher.Publish(order);

		return Ok();
	}

	[HttpGet]
	[Route("polly")]
	public async Task<IActionResult> Polly()
	{
		//await myHttpClient.Execute();

		//return Ok();

		var count = 0;
		AsyncRetryPolicy<ProductCreatedEvent> retry = Policy<ProductCreatedEvent>
			.Handle<Exception>()
			//.OrResult<ProductCreatedEvent>(x => x.Name == "")
			.WaitAndRetryAsync(
			Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), retryCount: 5),
			onRetry: (exception, waitTime, ctx) =>
			{
				count++;
				Console.WriteLine($"Exception: " + exception.Exception.GetType());
				Console.WriteLine($"Wait time: " + waitTime); // 0:02 | 0:04 | 0:08 | 0:16 | ...
			});
		//.WaitAndRetryAsync(
		//retryCount: 5,
		//retryAttempt =>
		//{
		//	return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)); // retryAttempt = 1,2,3,4,5
		//},
		//onRetry: (exception, waitTime, ctx) =>
		//{
		//	Console.WriteLine($"Exception: " + exception.Exception.GetType());
		//	Console.WriteLine($"Wait time: " + waitTime); // 0:02 | 0:04 | 0:08 | 0:16 | ...
		//});

		var curcuit = Policy.Handle<Exception>()
			.CircuitBreakerAsync(
				exceptionsAllowedBeforeBreaking: 3,
				durationOfBreak: TimeSpan.FromSeconds(10),
				onBreak: (exception, waitTime) =>
				{
					Console.WriteLine("Break: " + exception.GetType() + " || " + "Waittime: " + waitTime);
					// action on break
				},
				onReset: () =>
				{
					Console.WriteLine("Reset");
					// action on reset
				});

		AsyncPolicyWrap<ProductCreatedEvent> finalPolicy = retry.WrapAsync(curcuit);

		var result = await finalPolicy.ExecuteAndCaptureAsync(async () =>
		{
			await Task.Delay(0);

            await Console.Out.WriteLineAsync("Try#: " + count);

			if (count >= 4)
			{
                await Console.Out.WriteLineAsync("Returning");
                return new ProductCreatedEvent() { Name = "PIZDA" };
			}

            //return new ProductCreatedEvent() { Name = "Fikus" };
            throw new Exception();
		});

		Console.WriteLine(result.Outcome == OutcomeType.Successful ? "OK" : "FAIL");

		return Ok();


		//var result = await retry.ExecuteAndCaptureAsync(async () =>
		//{
		//	count++;
		//	await Task.Delay(1);

		//	if(count == 4)
		//		return new ProductCreatedEvent() { Name = "PIZDA" };

		//	//return new ProductCreatedEvent() { Name = "Fikus" };
		//	throw new Exception();
		//});


		//var res = result.Result;

		//if (result.Outcome == OutcomeType.Successful)
		//	await Console.Out.WriteLineAsync("Fuck yeah");

		//if (result.Outcome == OutcomeType.Failure)
		//	await Console.Out.WriteLineAsync("sad face");

		//return Ok();
	}

	[HttpGet]
	[Route("broadcast")]
	public async Task<IActionResult> Broadcast(string message)
	{
		await hubContext.Clients.All.RecieveMessage(message);

		return Ok();
	}
}
