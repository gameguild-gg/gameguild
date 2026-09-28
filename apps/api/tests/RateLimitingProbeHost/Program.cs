using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using GameGuild.API;
using GameGuild.API.Core.Middleware;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Resources;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

if (args.Length != 0)
{
    throw new ArgumentException("The rate-limiting probe host does not accept command-line configuration.");
}

var redisEndpoint = Environment.GetEnvironmentVariable("GAMEGUILD_RATE_LIMIT_REDIS_ENDPOINT")
    ?? throw new InvalidOperationException("The Redis endpoint environment variable is required.");
var certificatePath = Environment.GetEnvironmentVariable("GAMEGUILD_RATE_LIMIT_HTTPS_CERTIFICATE_PATH")
    ?? throw new InvalidOperationException("The HTTPS certificate path environment variable is required.");
var certificatePassword = Environment.GetEnvironmentVariable("GAMEGUILD_RATE_LIMIT_HTTPS_CERTIFICATE_PASSWORD")
    ?? throw new InvalidOperationException("The HTTPS certificate password environment variable is required.");
var httpsHost = Environment.GetEnvironmentVariable("GAMEGUILD_RATE_LIMIT_HTTPS_HOST") ?? "127.0.0.1";
if (!int.TryParse(
        Environment.GetEnvironmentVariable("GAMEGUILD_RATE_LIMIT_HTTPS_PORT"),
        System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture,
        out var port)
    || port is < 1 or > 65535)
{
    throw new ArgumentOutOfRangeException(nameof(port), "HTTPS port must be between 1 and 65535.");
}

if (!int.TryParse(
        Environment.GetEnvironmentVariable("GAMEGUILD_RATE_LIMIT_REQUEST_LIMIT"),
        System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture,
        out var requestLimit)
    || requestLimit <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(requestLimit), "The request limit must be greater than zero.");
}

using var serverCertificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, certificatePassword);
await using var redis = await ConnectionMultiplexer.ConnectAsync(redisEndpoint);
var builder = WebApplication.CreateBuilder([]);
builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureHttpsDefaults(httpsOptions => httpsOptions.ServerCertificate = serverCertificate));
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

await app.RunAsync($"https://{httpsHost}:{port}");
