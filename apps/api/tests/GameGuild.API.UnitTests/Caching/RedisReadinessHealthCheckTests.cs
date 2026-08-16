using FluentAssertions;
using GameGuild.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace GameGuild.API.UnitTests.Caching;

public sealed class RedisReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsConnected_ShouldReturnHealthy()
    {
        var database = new Mock<IDatabase>();
        database.Setup(value => value.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(3));

        var redis = new Mock<IConnectionMultiplexer>();
        redis.SetupGet(value => value.IsConnected).Returns(true);
        redis.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(database.Object);

        var result = await new RedisReadinessHealthCheck(redis.Object)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["latencyMilliseconds"].Should().Be(3d);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsDisconnected_ShouldReturnUnhealthy()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.SetupGet(value => value.IsConnected).Returns(false);

        var result = await new RedisReadinessHealthCheck(redis.Object)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingFails_ShouldReturnUnhealthy()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.SetupGet(value => value.IsConnected).Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "offline"));

        var result = await new RedisReadinessHealthCheck(redis.Object)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<RedisConnectionException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ShouldPropagateCancellation()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var action = () => new RedisReadinessHealthCheck(redis.Object)
            .CheckHealthAsync(new HealthCheckContext(), cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void SetupMemoryCaching_ShouldRegisterRedisReadinessOnlyWhenEnabled(bool enableHealthChecks, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Enabled"] = "true",
                ["Redis:ConnectionString"] = "localhost:6379",
                ["Redis:EnableHealthChecks"] = enableHealthChecks.ToString()
            })
            .Build();
        var services = new ServiceCollection();

        services.SetupMemoryCaching(configuration, null);

        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        registrations.Any(registration => registration.Name == "redis").Should().Be(expected);
    }
}
