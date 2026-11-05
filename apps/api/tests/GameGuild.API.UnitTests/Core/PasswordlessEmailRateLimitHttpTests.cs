using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Asp.Versioning;
using FluentAssertions;
using GameGuild.API.Core.Middleware;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using GameGuild.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.API.UnitTests.Core;

public sealed class PasswordlessEmailRateLimitHttpTests
{
    private const string AccountA = "00000000-0000-0000-0000-000000000010";
    private const string AccountB = "00000000-0000-0000-0000-000000000020";
    private const string AccountC = "00000000-0000-0000-0000-000000000030";

    [Theory]
    [InlineData(false, "magic-link")]
    [InlineData(false, "email-code")]
    [InlineData(true, "magic-link")]
    [InlineData(true, "email-code")]
    public async Task EmailIssuance_SharesIpBudgetAcrossAccountsRecipientsAndEndpoints(bool redisEnabled, string flow)
    {
        var dispatched = new ConcurrentQueue<string>();
        await using var app = await CreateAppAsync(redisEnabled, dispatched);
        using var client = app.GetTestClient();

        using var first = await SendAsync(client, flow, "192.0.2.10", AccountA, "first@example.com");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        using var rotatedAccount = await SendAsync(client, flow, "192.0.2.10", AccountB, "second@example.com");
        rotatedAccount.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rotatedAccount.Headers.RetryAfter.Should().NotBeNull();

        var otherFlow = flow == "magic-link" ? "email-code" : "magic-link";
        using var rotatedEndpoint = await SendAsync(client, otherFlow, "192.0.2.10", AccountC, "third@example.com");
        rotatedEndpoint.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var anonymous = await SendAsync(client, flow, "192.0.2.10", null, "anonymous@example.com");
        anonymous.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        dispatched.Should().Equal("first@example.com");

        // A different client address gets its own budget even for the same account.
        using var differentIp = await SendAsync(client, flow, "192.0.2.11", AccountA, "fourth@example.com");
        differentIp.StatusCode.Should().Be(HttpStatusCode.OK);
        dispatched.Should().Equal("first@example.com", "fourth@example.com");
    }

    [Theory]
    [InlineData(false, "192.0.2.10", "::ffff:192.0.2.10")]
    [InlineData(false, "::ffff:192.0.2.10", "192.0.2.10")]
    [InlineData(true, "192.0.2.10", "::ffff:192.0.2.10")]
    [InlineData(true, "::ffff:192.0.2.10", "192.0.2.10")]
    public async Task EmailIssuance_CanonicalizesMappedIpv4AcrossConnectionRepresentations(bool redisEnabled, string firstIp, string alternateIp)
    {
        var dispatched = new ConcurrentQueue<string>();
        await using var app = await CreateAppAsync(redisEnabled, dispatched);
        using var client = app.GetTestClient();
        using var first = await SendAsync(client, "magic-link", firstIp, AccountA, "first@example.com");
        using var alternate = await SendAsync(client, "email-code", alternateIp, AccountB, "second@example.com");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        alternate.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        dispatched.Should().Equal("first@example.com");
        using var ipv6 = await SendAsync(client, "email-code", "2001:db8::10", AccountA, "ipv6@example.com");
        ipv6.StatusCode.Should().Be(HttpStatusCode.OK);
        dispatched.Should().Equal("first@example.com", "ipv6@example.com");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnonymousIssuance_CannotRenewBudgetByChangingEmailOrSpoofingForwardingHeaders(bool redisEnabled)
    {
        var dispatched = new ConcurrentQueue<string>();
        await using var app = await CreateAppAsync(redisEnabled, dispatched);
        using var client = app.GetTestClient();
        using var first = await SendAsync(client, "magic-link", "192.0.2.10", null, "first@example.com");
        using var second = await SendAsync(client, "email-code", "192.0.2.10", null, "second@example.com");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        dispatched.Should().Equal("first@example.com");
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string flow, string ip, string? account, string email)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/auth/{flow}:request")
        {
            Content = JsonContent.Create(new { email })
        };
        request.Headers.Add("Test-Remote-Ip", ip);
        if (account is not null)
        {
            request.Headers.Add("Test-Account", account);
        }

        // These untrusted headers must not select a fresh limiter partition.
        request.Headers.Add("X-Forwarded-For", email == "first@example.com" ? "203.0.113.21" : "203.0.113.22");
        request.Headers.Add("X-Real-IP", "203.0.113.99");
        return await client.SendAsync(request);
    }

    private static async Task<WebApplication> CreateAppAsync(bool redisEnabled, ConcurrentQueue<string> dispatched)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:Enabled"] = redisEnabled.ToString() });
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning(options => options.ApiVersionReader = new UrlSegmentApiVersionReader()).AddMvc();
        builder.Services.AddControllers().ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Clear();
            manager.ApplicationParts.Add(new AuthControllerPart());
        });
        var options = RateLimitingOptions.CreateDefault();
        options.AuthenticationRequestsPerMinute = 1;
        options.QueueLimit = 0;
        options.EnableProgressivePenalties = false;
        options.TrustedProxyAddresses = [];
        options.TrustedProxyNetworks = [];
        builder.Services.SetupRateLimiting(builder.Configuration, options);

        // Exercise the production distributed middleware and its key/limit selection.
        // This deterministic store double does not claim to execute a Redis server.
        var counts = new ConcurrentDictionary<string, int>();
        var limiter = new Mock<IDistributedRateLimiter>();
        limiter.Setup(service => service.IsAllowedFixedWindowAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, int limit, TimeSpan _, CancellationToken _) => counts.AddOrUpdate(key, 1, (_, count) => count + 1) <= limit);
        builder.Services.AddSingleton(limiter.Object);
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<RequestMagicLinkCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MagicLinkRequestResult>, CancellationToken>((command, _) => dispatched.Enqueue(((RequestMagicLinkCommand)command).Email))
            .ReturnsAsync(new MagicLinkRequestResult());
        sender.Setup(service => service.Send(It.IsAny<RequestEmailCodeCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<EmailCodeRequestResult>, CancellationToken>((command, _) => dispatched.Enqueue(((RequestEmailCodeCommand)command).Email))
            .ReturnsAsync(new EmailCodeRequestResult());
        builder.Services.AddSingleton(sender.Object);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            // Only this test host synthesizes connection and authenticated actor state.
            context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers["Test-Remote-Ip"].ToString());
            if (context.Request.Headers.TryGetValue("Test-Account", out var account))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account.ToString())], "test"));
            }
            await next(context);
        });
        app.UseForwardedHeaders();
        app.UseRouting();
        app.UseAuthorization();
        app.UseRateLimiter();
        if (redisEnabled)
        {
            app.UseMiddleware<RedisEndpointRateLimitingMiddleware>();
        }
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private sealed class AuthControllerPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(AuthControllerPart);
        public IEnumerable<TypeInfo> Types => [typeof(AuthController).GetTypeInfo()];
    }
}
