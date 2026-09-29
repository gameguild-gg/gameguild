using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using GameGuild.API.Core.Middleware;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.API.UnitTests.Core;

public sealed class RedisEndpointRateLimitingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesAuthenticatedApiKeyIdForGlobalBucketInsteadOfOwnerUserId()
    {
        var options = new RateLimitingOptions
        {
            Limit = 1,
            Period = TimeSpan.FromMinutes(1),
            ExemptPaths = []
        };
        var limiter = new Mock<IDistributedRateLimiter>();
        limiter.Setup(service => service.IsAllowedFixedWindowAsync(
                "api:global:api-key:key-123",
                options.Limit,
                options.Period,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "owner-user-1"),
                new Claim("auth_method", "api_key"),
                new Claim("api_key_id", "key-123")
            ], "api-key"))
        };
        context.Request.Path = "/api/v1/projects";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.30");
        var nextCalled = false;
        var middleware = new RedisEndpointRateLimitingMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, limiter.Object, options, new RateLimitAccessOptions());

        nextCalled.Should().BeTrue();
        limiter.VerifyAll();
    }

    [Fact]
    public async Task InvokeAsync_FailClosedStoreFailure_Returns503ProblemDetailsWithoutCallingEndpoint()
    {
        var limiter = new Mock<IDistributedRateLimiter>();
        limiter
            .Setup(service => service.IsAllowedFixedWindowAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RateLimitBackendUnavailableException(
                "fixed-window admission",
                new InvalidOperationException("redis offline")));

        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            TraceIdentifier = "rate-limit-store-failure"
        };
        context.Request.Path = "/api/v1/projects";
        context.Response.Body = new MemoryStream();
        var nextCalled = false;
        var middleware = new RedisEndpointRateLimitingMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            context,
            limiter.Object,
            new RateLimitingOptions { EnableRateLimiting = true },
            new RateLimitAccessOptions());

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.Should().Be("application/problem+json");
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status503ServiceUnavailable);
        body.RootElement.GetProperty("errorCode").GetString().Should().Be("rate_limit_store_unavailable");
        body.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/projects");
        nextCalled.Should().BeFalse();
    }
}
