using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Security.Claims;
using RabbitMQ.Identity;
using RabbitMQ.Hubs;
using RabbitMQ.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

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

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//				.AddJwtBearer(options =>
//				{
//					options.TokenValidationParameters = new TokenValidationParameters()
//					{
//						ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),
//						ValidAudience = configuration.GetValue<string>("JwtSettings:Audience"),
//						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:Key"))),
//						ValidateIssuer = true,
//						ValidateAudience = true,
//						ValidateLifetime = true,
//						ValidateIssuerSigningKey = false
//					};
//				});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.RequireHttpsMetadata = false;
					options.TokenValidationParameters = new TokenValidationParameters
					{
						// Disable signature validation
						RequireSignedTokens = false,
						ValidateIssuerSigningKey = false,
						SignatureValidator = (string token, TokenValidationParameters _) => new JwtSecurityToken(token),

						ValidateIssuer = false, 
						ValidateAudience = false,
						ValidateActor = false,
						ValidateLifetime = true
					};
				});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("TestPolicy", policy => policy.RequireClaim(ClaimTypes.Name, "Vasya"));
	options.AddPolicy(IdentityData.AdminPolicyName, policy => policy.RequireClaim(IdentityData.AdminClaimName, "true"));

	options.AddPolicy("EditPolicy", policy =>
		policy.Requirements.Add(new SameOwnerRequirement()));
	options.AddPolicy(IdentityData.AdminPolicyName, policy => policy.RequireRole("admin"));
});

builder.Services.AddSingleton<IAuthorizationHandler, MyResourcceAuthorizationHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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


var app = builder.Build();

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
