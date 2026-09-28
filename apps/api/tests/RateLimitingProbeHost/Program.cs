using System.Security.Claims;
using GameGuild.API;
using GameGuild.API.Core.Middleware;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Resources;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

if (args.Length != 3)
    throw new ArgumentException("Expected Redis endpoint, HTTP port, and request limit.");

var redisEndpoint = args[0];
var port = int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
var requestLimit = int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);

await using var redis = await ConnectionMultiplexer.ConnectAsync(redisEndpoint);
var builder = WebApplication.CreateBuilder([]);
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:Enabled"] = "true" })
    .Build();
var options = RateLimitingOptions.CreateDefault();
options.Limit = requestLimit;
options.Period = TimeSpan.FromMinutes(1);
options.ExemptPaths = [];
builder.Services.SetupRateLimiting(configuration, options);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton<IDistributedRateLimiter, RedisDistributedRateLimiter>();

var app = builder.Build();
app.UseRouting();
app.Use(async (context, next) =>
{
    var userId = context.Request.Headers["X-Test-User"].ToString();
    if (!string.IsNullOrWhiteSpace(userId))
    {
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: "rate-limit-probe"));
    }

    await next(context).ConfigureAwait(false);
});
app.UseRateLimiter();
app.UseMiddleware<RedisEndpointRateLimitingMiddleware>();
app.MapGet("/limited", () => Results.NoContent());
app.MapGet("/healthz", () => Results.Ok())
    .WithMetadata(new DisableRateLimitingAttribute());

await app.RunAsync($"http://127.0.0.1:{port}");
