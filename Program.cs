using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using RabbitMQ.SagaStateMachine;
using System.Security.Claims;
using RabbitMQ.Consumers;
using RabbitMQ.Identity;
using RabbitMQ.Services;
using System.Reflection;
using RabbitMQ.Models;
using Polly.Timeout;
using RabbitMQ.Hubs;
using RabbitMQ.Test;
using System.Text;
using MassTransit;
using Polly;
using RabbitMQ.Controllers;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddSignalR(opt =>
{
	//opt.AddFilter<AuthHubFilter>();
});

builder.Services.AddRateLimiter(rateOptions =>
{
	rateOptions.AddPolicy(policyName: "fixedIP", ctx =>
	{
		// if (ctx.Connection.RemoteIpAddress?.ToString() == "")
		// if(ctx.User ==  null)
		ctx.Request.Headers.TryGetValue("x-header", out var value);

		return RateLimitPartition.GetFixedWindowLimiter(partitionKey: value, key =>
		{
			return new FixedWindowRateLimiterOptions()
			{
				Window = TimeSpan.FromSeconds(10),
				PermitLimit = 2,
				QueueLimit = 0,
			};
		});
	});

	rateOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

	rateOptions.AddFixedWindowLimiter(policyName: "fixed", options =>
	{
		options.Window = TimeSpan.FromSeconds(10); // just a time frame
		options.PermitLimit = 3; // how many requests are allowed within said window
		options.QueueLimit = 0; // how many requests can be stored when permitLimit is reached
		options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
	});

	rateOptions.AddSlidingWindowLimiter("sliding", options =>
	{
		options.Window = TimeSpan.FromSeconds(15);
		options.SegmentsPerWindow = 3;
		options.PermitLimit = 15;
	});

	rateOptions.AddTokenBucketLimiter("token", options =>
	{
		options.TokenLimit = 5; // 100 requests total, like at all
		options.ReplenishmentPeriod = TimeSpan.FromSeconds(10); // how fast specified number of tokens below are replenished
		options.TokensPerPeriod = 10; // how many tokens are replenished within timespan value from above
	});

	rateOptions.AddConcurrencyLimiter("concurrent", options =>
	{
		options.PermitLimit = 5; // # of concurrent requests to the api
	});
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					// here can be getting IOptions<JwtSettings> for these values.
					// as well as that JwtSettings class can be validated on start
					options.TokenValidationParameters = new TokenValidationParameters()
					{
						ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),
						ValidAudience = configuration.GetValue<string>("JwtSettings:Audience"),
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:Key"))),
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true
					};
				});

//builder.Services.AddAuthorization();
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("TestPolicy", policy => policy.RequireClaim(ClaimTypes.Name, "Fikus"));
	options.AddPolicy(IdentityData.AdminPolicyName, policy => policy.RequireClaim(IdentityData.AdminClaimName, "true"));

	options.AddPolicy("EditPolicy", policy =>
		policy.Requirements.Add(new SameOwnerRequirement()));
	//options.AddPolicy(IdentityData.AdminPolicyName, policy => policy.RequireRole("admin"));
});

builder.Services.AddSingleton<IAuthorizationHandler, MyResourcceAuthorizationHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddHostedService<Consumer>();
//builder.Services.AddHostedService<ConsumerTwo>();
//builder.Services.AddHostedService<MotherConsumer>();
//builder.Services.AddSingleton<Publisher>();
//builder.Services.AddSingleton<RabbitConnection>();


builder.Services.AddScoped<IEventBus, EventBus>();

builder.Services.AddMassTransit(config =>
{
	config.SetKebabCaseEndpointNameFormatter();

	config.AddConsumers(Assembly.GetExecutingAssembly());


	//config.AddConsumer<ProductCreatedEventConsumer>();
	//config.AddConsumer<ProductCreatedEventConsumerTwo>().Endpoint(x =>
	//{
	//	x.Temporary = true;
	//	x.Name = "test";
	//});

	config.AddSagaStateMachine<OrderStateMachine, OrderState>().InMemoryRepository();
	config.AddSagaStateMachine<OrderStateMachineTwo, OrderStateTwo>(opt =>
	{

		opt.UseDelayedRedelivery(r => r.Intervals(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30)));
		opt.UseMessageRetry(retry => retry.Interval(5, TimeSpan.FromSeconds(1)));
		opt.UseInMemoryOutbox();

	}).InMemoryRepository();
	//config.AddSaga<OrderSaga>().InMemoryRepository();

	config.UsingRabbitMq((ctx, rabbit) =>
	{
		//rabbit.UseExceptionLogger();
		//rabbit.UseMessageFilter();

		//rabbit.UseConsumeFilter(typeof(MessageFilter<>), ctx);
		//rabbit.UseSendFilter(typeof(MySendFilter<>), ctx);

		//rabbit.UseMessageScope(ctx);
		//rabbit.UsePublishFilter(typeof(MyPublishFilter<>), ctx);

		rabbit.Host(new Uri("amqps://xpymutom:TKDT5H__76nWpMIUSRWs6oitzwMYpdRf@chimpanzee.rmq.cloudamqp.com/xpymutom"), p =>
		{
			p.Username("xpymutom");
			p.Password("TKDT5H__76nWpMIUSRWs6oitzwMYpdRf");
		});

		rabbit.Message<ProductCreatedEvent>(x =>
		{
			x.SetEntityName(nameof(ProductCreatedEvent));
		});

		rabbit.Message<CheckOrderStatus>(x =>
		{
			x.SetEntityName(nameof(CheckOrderStatus));
		});

		rabbit.Send<ProductCreatedEvent>(x =>
		{
			// Используем своство класса в качестве routing-key
			x.UseRoutingKeyFormatter(context => context.Message.Name);

			// Задаем в качестве CorrelationId свойства класса
			x.UseCorrelationId(message => message.CorrelationId);
		});

		rabbit.ReceiveEndpoint("product-created-event", opt =>
		{
			//opt.ConfigureConsumeTopology = false;
			opt.AutoDelete = true;

			opt.Bind("product-event", x =>
			{
				//x.Durable = false;
				//x.AutoDelete = true;
				x.ExchangeType = "direct";
				x.RoutingKey = "Fikus";
			});

			//opt.Bind<ProductCreatedEvent>();
			//opt.Bind(exchangeName: "RabbitMQ.Models:ProductCreatedEvent");

			//opt.Consumer<ProductCreatedSecondEventConsumer>();
			opt.ConfigureConsumer<ProductCreatedSecondEventConsumer>(ctx, opt =>
			{
				opt.UseMessageRetry(x => x.Immediate(5));
				opt.UseInMemoryOutbox();
			});

			opt.ConfigureConsumer<ProductCreatedFirstEventConsumer>(ctx, opt => opt.UseInMemoryOutbox());

			//opt.ConfigureConsumer<ProductCreatedEventConsumerTwo>(ctx);
			//opt.ConfigureConsumers(ctx);
		});

		rabbit.ReceiveEndpoint("create-something-command-endpoint", opt =>
		{
			opt.AutoDelete = true;

			// предотвращает создание exchange для потребляемых типов (events/commands)
			opt.ConfigureConsumeTopology = false;

			opt.ConfigureConsumer<CreateSomethingCommandConsumer>(ctx, opt =>
			{
				opt.UseMessageRetry(x => x.Immediate(5));
				opt.UseInMemoryOutbox();
			});
		});

		rabbit.PrefetchCount = 100;
		rabbit.ConcurrentMessageLimit = 10;

		//EndpointConvention.Map<ProductCreatedEvent>(new Uri("exchange:product-created-event?autoDelete=true"));
		EndpointConvention.Map<ProductCreatedEvent>(new Uri("exchange:product-event?type=direct"));

		EndpointConvention.Map<CreateSomethingCommand>(
			new Uri("exchange:create-something-command-endpoint?autoDelete=true"));

		rabbit.ConfigureEndpoints(ctx);
	});
});


builder.Services.AddApiVersioning(options =>
{
	// if api version is not specified - gives 400 error if false. Default = false.
	options.AssumeDefaultVersionWhenUnspecified = true;
	options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0); // v1.0
																			   // gives information about available versions for API if true. Default = false.
	options.ReportApiVersions = true;
	options.ApiVersionReader = ApiVersionReader.Combine(
			new HeaderApiVersionReader("x-version"), // #1, the only used 
			new QueryStringApiVersionReader("api-version"), // #2, occasionally used
			new MediaTypeApiVersionReader("version")); // not really used
});

builder.Services.AddVersionedApiExplorer(options =>
{
	options.GroupNameFormat = "'v'VVV";
	options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddHttpClient<MyHttpClient>(options =>
{
	options.BaseAddress = new Uri("http://localhost:5000");
});


builder.Services.AddHttpClient<SomeService>(x =>
{
	x.BaseAddress = new Uri("http://localhost:5000");
})
	.AddTransientHttpErrorPolicy(builder =>
{
	return builder
	.Or<TimeoutRejectedException>()
	.WaitAndRetryAsync(
		retryCount: 5,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
		onRetry: (outcome, waitTime, ctx) =>
		{
			// action on retry
		});
})
	.AddTransientHttpErrorPolicy(builder =>
{
	return builder
	.Or<TimeoutRejectedException>()
	.CircuitBreakerAsync(
		handledEventsAllowedBeforeBreaking: 3,
		durationOfBreak: TimeSpan.FromSeconds(15),
		onBreak: (outcome, waitTime) =>
		{
			// action on break
		},
		onReset: () =>
		{
			// action on reset
		});
})
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(seconds: 1));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapHub<ChatHub>("/chat");


app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

//app.UseRateLimiter(new RateLimiterOptions()
//{
//	GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
//	{
//		return RateLimitPartition.GetConcurrencyLimiter<string>("CONC", _ =>
//		{
//			return new ConcurrencyLimiterOptions()
//			{
//				PermitLimit = 2,
//				QueueLimit = 5
//			};
//		});

//		return RateLimitPartition.GetFixedWindowLimiter<string>("window", _ =>
//		{
//			return new FixedWindowRateLimiterOptions()
//			{
//				Window = TimeSpan.FromSeconds(10),
//				PermitLimit = 3,
//				QueueLimit = 0,
//			};
//		});
//	}),
//	RejectionStatusCode = StatusCodes.Status429TooManyRequests
//});


app.MapControllers();

app.Run();
