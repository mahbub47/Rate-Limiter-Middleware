using RateLimiterMiddleware.Config;
using RateLimiterMiddleware.Identity;
using RateLimiterMiddleware.Middlewares;
using RateLimiterMiddleware.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!
    )
);

builder.Services.AddScoped<IRateLimiterService, SlidingWindowRateLimiterService>();

builder.Services.Configure<RateLimiterConfig>(
    builder.Configuration.GetSection("RateLimiter")
);

builder.Services.AddTransient<IClientIdentityResolver, IpClientIdentityResolver>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<RateLimiterMiddleware.Middlewares.RateLimiterMiddleware>();

app.MapControllers();

app.Run();
