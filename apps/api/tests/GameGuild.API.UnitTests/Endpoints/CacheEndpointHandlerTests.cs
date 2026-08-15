using System.Reflection;
using FluentAssertions;
using GameGuild.API.Endpoints;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;

namespace GameGuild.API.UnitTests.Endpoints;

public sealed class CacheEndpointHandlerTests
{
    [Fact]
    public void MapEndpoint_ShouldRegisterEveryCacheOperation()
    {
        var app = WebApplication.CreateBuilder().Build();

        new CacheEndpoints().MapEndpoint(app);

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
        routes.Should().BeEquivalentTo(
            "/cache/health",
            "/cache/test",
            "/cache/clear/{pattern}",
            "/cache/stats");
    }

    [Theory]
    [InlineData(false, null, "Memory", "Available")]
    [InlineData(true, false, "Redis", "Unavailable")]
    [InlineData(true, true, "Redis", "Available")]
    public async Task GetCacheStats_ShouldReportConfiguredProviderAndConnectivity(
        bool redisEnabled,
        bool? redisConnected,
        string expectedProvider,
        string expectedStatus)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Enabled"] = redisEnabled.ToString(),
                ["Redis:InstanceName"] = "test-instance"
            })
            .Build();
        var services = new ServiceCollection();
        if (redisConnected.HasValue)
        {
            var multiplexer = new Mock<IConnectionMultiplexer>();
            multiplexer.SetupGet(value => value.IsConnected).Returns(redisConnected.Value);
            services.AddSingleton(multiplexer.Object);
        }

        await using var provider = services.BuildServiceProvider();

        var result = await InvokeGetCacheStats(configuration, provider);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        var stats = GetProperty<object>(value, "Stats");
        GetProperty<string>(stats, "Type").Should().Be(expectedProvider);
        GetProperty<string>(stats, "Status").Should().Be(expectedStatus);
        GetProperty<bool>(stats, "RedisEnabled").Should().Be(redisEnabled);
        GetProperty<string>(stats, "InstanceName").Should().Be("test-instance");
        stats.GetType().GetProperty("RedisConnected")!.GetValue(stats).Should().Be(redisConnected);
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldUsePatternCacheServiceAndReportRemovedCount()
    {
        var cache = new Mock<IPatternCacheService>();
        cache
            .Setup(x => x.RemoveByPatternAsync("tenant:42:*", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:42:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<int>(value, "RemovedCount").Should().Be(3);
        GetProperty<string>(value, "Pattern").Should().Be("tenant:42:*");
        cache.Verify(x => x.RemoveByPatternAsync("tenant:42:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldReturnNotImplementedWhenCacheDoesNotSupportPatternRemoval()
    {
        var cache = new Mock<ICacheService>();

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<string>(value, "Pattern").Should().Be("tenant:*");
        GetProperty<string>(value, "Message").Should().Contain("does not support pattern-based cache clearing");
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldReturnNotImplementedWhenPatternServiceRejectsPattern()
    {
        var cache = new Mock<IPatternCacheService>();
        cache
            .Setup(x => x.RemoveByPatternAsync("tenant:*", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Wildcard scan is not available."));

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<string>(value, "Error").Should().Be("Wildcard scan is not available.");
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldRemoveExactKeyWhenProviderDoesNotSupportPatterns()
    {
        var cache = new Mock<ICacheService>();

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:42", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
        cache.Verify(x => x.RemoveAsync("tenant:42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldRejectBlankPattern()
    {
        var cache = new Mock<ICacheService>();

        var result = await CacheEndpointHandlers.ClearCacheByPattern(" ", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData(true, "Healthy")]
    [InlineData(false, "Unhealthy")]
    public async Task GetCacheHealth_ShouldReportRoundTripResult(bool roundTripMatches, string expectedStatus)
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roundTripMatches ? "health_check_value" : "different");
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await InvokePrivateEndpoint("GetCacheHealth", cache.Object, CancellationToken.None);

        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<string>(value, "Status").Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException), "configuration error")]
    [InlineData(typeof(TimeoutException), "timeout")]
    public async Task GetCacheHealth_ShouldDescribeExpectedProviderFailures(Type exceptionType, string expectedMessage)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "offline")!;
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await InvokePrivateEndpoint("GetCacheHealth", cache.Object, CancellationToken.None);

        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<string>(value, "Status").Should().Be("Unhealthy");
        GetProperty<string>(value, "Message").Should().Contain(expectedMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestCacheOperations_ShouldReportEveryOperation(bool successfulRoundTrip)
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.SetupSequence(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successfulRoundTrip ? new object() : null)
            .ReturnsAsync(successfulRoundTrip ? null : new object());
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await InvokePrivateEndpoint("TestCacheOperations", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task TestCacheOperations_WhenProviderRejectsWrite_ShouldReturnBadRequest()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("offline"));

        var result = await InvokePrivateEndpoint("TestCacheOperations", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task ClearCacheByPattern_WhenProviderRejectsPattern_ShouldReturnBadRequest(Type exceptionType)
    {
        var cache = new Mock<IPatternCacheService>();
        cache.Setup(x => x.RemoveByPatternAsync("tenant:*", It.IsAny<CancellationToken>()))
            .ThrowsAsync((Exception)Activator.CreateInstance(exceptionType, "invalid")!);

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    private static T GetProperty<T>(object? value, string name)
    {
        value.Should().NotBeNull();
        var property = value!.GetType().GetProperty(name);
        property.Should().NotBeNull();
        return property!.GetValue(value).Should().BeAssignableTo<T>().Subject;
    }

    private static Task<IResult> InvokeGetCacheStats(IConfiguration configuration, IServiceProvider services)
    {
        var endpointType = typeof(CacheEndpointHandlers).Assembly
            .GetType("GameGuild.API.Endpoints.CacheEndpoints", throwOnError: true)!;
        var method = endpointType.GetMethod("GetCacheStats", BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!.Invoke(null, [configuration, services]).Should().BeAssignableTo<Task<IResult>>().Subject;
    }

    private static Task<IResult> InvokePrivateEndpoint(string methodName, params object?[] arguments)
    {
        var endpointType = typeof(CacheEndpointHandlers).Assembly
            .GetType("GameGuild.API.Endpoints.CacheEndpoints", throwOnError: true)!;
        var method = endpointType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!.Invoke(null, arguments).Should().BeAssignableTo<Task<IResult>>().Subject;
    }
}
