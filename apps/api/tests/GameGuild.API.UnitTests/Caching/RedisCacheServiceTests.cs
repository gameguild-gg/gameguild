using System.Net;
using System.Text;
using FluentAssertions;
using GameGuild.API;
using GameGuild.Configuration.InfrastructureLayer.RedisCaching;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using StackExchange.Redis;

namespace GameGuild.API.UnitTests.Caching;

public sealed class RedisCacheServiceTests
{
    [Fact]
    public async Task GetAsync_WhenValueExists_ShouldDeserializeValue()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(value => value.GetAsync("profile", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("{\"name\":\"Ada\"}"));
        var service = CreateService(cache: cache);

        var result = await service.GetAsync<TestValue>("profile");

        result.Should().BeEquivalentTo(new TestValue("Ada"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_WhenValueIsMissing_ShouldReturnDefault(string? value)
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(item => item.GetAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(value is null ? null : Encoding.UTF8.GetBytes(value));
        var service = CreateService(cache: cache);

        var result = await service.GetAsync<TestValue>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeValueAndApplyAbsoluteExpiration()
    {
        var cache = new Mock<IDistributedCache>();
        var expiration = TimeSpan.FromMinutes(12);
        var service = CreateService(cache: cache);

        await service.SetAsync("profile", new TestValue("Ada"), expiration);

        cache.Verify(value => value.SetAsync(
            "profile",
            It.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == "{\"name\":\"Ada\"}"),
            It.Is<DistributedCacheEntryOptions>(options => options.AbsoluteExpirationRelativeToNow == expiration),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveExactKey()
    {
        var cache = new Mock<IDistributedCache>();
        var service = CreateService(cache: cache);

        await service.RemoveAsync("profile");

        cache.Verify(value => value.RemoveAsync("profile", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenPatternHasNoWildcard_ShouldRemoveExactKey()
    {
        var cache = new Mock<IDistributedCache>();
        var service = CreateService(cache: cache);

        var removed = await service.RemoveByPatternAsync("profile");

        removed.Should().Be(1);
        cache.Verify(value => value.RemoveAsync("profile", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("GameGuild:", "user:*", "GameGuild:user:*")]
    [InlineData("GameGuild:", "GameGuild:user:*", "GameGuild:user:*")]
    [InlineData("", "user:?", "user:?")]
    public async Task RemoveByPatternAsync_ShouldUseExpectedRedisPattern(
        string instanceName,
        string pattern,
        string expectedPattern)
    {
        var database = new Mock<IDatabase>();
        database.SetupGet(value => value.Database).Returns(0);
        var server = new Mock<IServer>();
        server.SetupGet(value => value.IsConnected).Returns(true);
        server.Setup(value => value.Keys(
                0,
                It.Is<RedisValue>(redisPattern => redisPattern == expectedPattern),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns([]);
        var redis = CreateRedis(database, server);
        var service = CreateService(redis: redis, instanceName: instanceName);

        var removed = await service.RemoveByPatternAsync(pattern);

        removed.Should().Be(0);
        server.VerifyAll();
    }

    [Fact]
    public async Task RemoveByPatternAsync_UsesRedisScanAndDeletesUniqueMatchingKeys()
    {
        var database = new Mock<IDatabase>();
        database.SetupGet(value => value.Database).Returns(0);
        database.Setup(value => value.KeyDeleteAsync(
                It.Is<RedisKey[]>(keys => keys.SequenceEqual(new RedisKey[]
                {
                    "GameGuild:user:1", "GameGuild:user:2"
                })),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);
        var server = new Mock<IServer>();
        server.SetupGet(value => value.IsConnected).Returns(true);
        server.Setup(value => value.Keys(
                0,
                "GameGuild:user:*",
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(["GameGuild:user:1", "GameGuild:user:1", "GameGuild:user:2"]);
        var redis = CreateRedis(database, server);
        var service = CreateService(redis: redis);

        var removed = await service.RemoveByPatternAsync("user:*");

        removed.Should().Be(2);
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenServerIsDisconnected_ShouldSkipIt()
    {
        var database = new Mock<IDatabase>();
        var server = new Mock<IServer>();
        server.SetupGet(value => value.IsConnected).Returns(false);
        var redis = CreateRedis(database, server);
        var service = CreateService(redis: redis);

        var removed = await service.RemoveByPatternAsync("user:*");

        removed.Should().Be(0);
        server.Verify(value => value.Keys(
            It.IsAny<int>(),
            It.IsAny<RedisValue>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenDeletionCountExceedsInt_ShouldThrowOverflowException()
    {
        var database = new Mock<IDatabase>();
        database.SetupGet(value => value.Database).Returns(0);
        database.Setup(value => value.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((long)int.MaxValue + 1);
        var server = new Mock<IServer>();
        server.SetupGet(value => value.IsConnected).Returns(true);
        server.Setup(value => value.Keys(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(["GameGuild:user:1"]);
        var service = CreateService(redis: CreateRedis(database, server));

        var action = () => service.RemoveByPatternAsync("user:*");

        await action.Should().ThrowAsync<OverflowException>();
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenCancelledBeforeScan_ShouldThrowOperationCanceledException()
    {
        var service = CreateService();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var action = () => service.RemoveByPatternAsync("user:*", cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PublicOperations_WhenKeyIsBlank_ShouldRejectInput()
    {
        var service = CreateService();

        await FluentActions.Invoking(() => service.GetAsync<string>(" ")).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => service.SetAsync(" ", "value", TimeSpan.FromMinutes(1))).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => service.RemoveAsync(" ")).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => service.RemoveByPatternAsync(" ")).Should().ThrowAsync<ArgumentException>();
    }

    private static RedisCacheService CreateService(
        Mock<IDistributedCache>? cache = null,
        Mock<IConnectionMultiplexer>? redis = null,
        string instanceName = "GameGuild:")
        => new(
            cache?.Object ?? Mock.Of<IDistributedCache>(),
            redis?.Object ?? Mock.Of<IConnectionMultiplexer>(),
            new RedisCachingOptions { InstanceName = instanceName });

    private static Mock<IConnectionMultiplexer> CreateRedis(Mock<IDatabase> database, Mock<IServer> server)
    {
        var endpoint = new DnsEndPoint("localhost", 6379);
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(database.Object);
        redis.Setup(value => value.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);
        redis.Setup(value => value.GetServer(endpoint, It.IsAny<object?>())).Returns(server.Object);
        return redis;
    }

    private sealed record TestValue(string Name);
}
