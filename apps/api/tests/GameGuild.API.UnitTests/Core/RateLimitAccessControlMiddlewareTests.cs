using System.Net;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using GameGuild.API.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace GameGuild.API.UnitTests.Core;

public sealed class RateLimitAccessControlMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DenylistTakesPrecedenceOverAllowlist()
    {
        var options = new RateLimitAccessOptions
        {
            AllowlistedUserIds = ["user-1"],
            DenylistedUserIds = ["user-1"]
        };
        options.Validate();
        var context = new DefaultHttpContext { TraceIdentifier = "trace-denylist" };
        context.Request.Path = "/api/blocked";
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            "test"));
        var nextCalled = false;
        var middleware = new RateLimitAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, options);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        context.Response.ContentType.Should().Be("application/problem+json");
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status403Forbidden);
        body.RootElement.GetProperty("errorCode").GetString().Should().Be("forbidden");
        body.RootElement.GetProperty("instance").GetString().Should().Be("/api/blocked");
        body.RootElement.GetProperty("traceId").GetString().Should().Be("trace-denylist");
        body.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_DeniesMappedIpv4Address()
    {
        var options = new RateLimitAccessOptions { DenylistedIpAddresses = ["192.0.2.31"] };
        options.Validate();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.0.2.31");
        var nextCalled = false;
        var middleware = new RateLimitAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, options);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();
    }
}
