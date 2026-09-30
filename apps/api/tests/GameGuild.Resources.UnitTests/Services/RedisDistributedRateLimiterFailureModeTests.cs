using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Resources;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace GameGuild.Resources.UnitTests.Services;

public sealed class RedisDistributedRateLimiterFailureModeTests
{
    [Fact]
    public async Task FixedWindow_DefaultFailureMode_AllowsWhenRedisFails()
    {
        var (redis, database) = CreateFailingRedis();
        var limiter = new RedisDistributedRateLimiter(redis.Object, NullLogger<RedisDistributedRateLimiter>.Instance);

        var allowed = await limiter.IsAllowedFixedWindowAsync("user:42", 10, TimeSpan.FromMinutes(1));

        allowed.Should().BeTrue();
        database.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SlidingWindow_DefaultFailureMode_AllowsWhenRedisFails()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailOpenLimiter(redis.Object);

        var allowed = await limiter.IsAllowedAsync("user:42", 10, TimeSpan.FromMinutes(1));

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task FixedWindow_FailClosed_ReportsBackendUnavailable()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);

        var act = () => limiter.IsAllowedFixedWindowAsync("user:42", 10, TimeSpan.FromMinutes(1));

        var exception = await act.Should().ThrowAsync<RateLimitBackendUnavailableException>();
        exception.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task TokenBucket_FailClosed_ReportsBackendUnavailable()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);

        var act = () => limiter.TryAcquireTokenBucketAsync(
            "user:42",
            tokenLimit: 10,
            tokensPerPeriod: 2,
            replenishmentPeriod: TimeSpan.FromMinutes(1));

        await act.Should().ThrowAsync<RateLimitBackendUnavailableException>();
    }

    [Fact]
    public async Task TokenBucket_DefaultFailureMode_AllowsWhenRedisFails()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailOpenLimiter(redis.Object);

        var decision = await limiter.TryAcquireTokenBucketAsync(
            "user:42",
            tokenLimit: 10,
            tokensPerPeriod: 2,
            replenishmentPeriod: TimeSpan.FromMinutes(1));

        decision.IsAllowed.Should().BeTrue();
        decision.RetryAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task SlidingWindow_FailClosed_ReportsBackendUnavailable()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);

        var act = () => limiter.IsAllowedAsync("user:42", 10, TimeSpan.FromMinutes(1));

        await act.Should().ThrowAsync<RateLimitBackendUnavailableException>();
    }

    [Fact]
    public async Task ConcurrencyLease_FailClosed_ReportsBackendUnavailable()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);

        var act = () => limiter.TryAcquireConcurrencyLeaseAsync(
            "user:42",
            maxConcurrent: 2,
            leaseDuration: TimeSpan.FromMinutes(1));

        await act.Should().ThrowAsync<RateLimitBackendUnavailableException>();
    }

    [Fact]
    public async Task ConcurrencyLease_DefaultFailureMode_AllowsWhenRedisFails()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailOpenLimiter(redis.Object);

        var decision = await limiter.TryAcquireConcurrencyLeaseAsync(
            "user:42",
            maxConcurrent: 2,
            leaseDuration: TimeSpan.FromMinutes(1));

        decision.IsAllowed.Should().BeTrue();
        decision.Lease.Should().BeNull();
    }

    [Fact]
    public async Task ActivePenalty_FailClosed_ReportsBackendUnavailable()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);

        var act = () => limiter.GetActivePenaltyAsync("user:42");

        await act.Should().ThrowAsync<RateLimitBackendUnavailableException>();
    }

    [Fact]
    public async Task ActivePenalty_DefaultFailureModeReturnsNoPenaltyWhenRedisFails()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailOpenLimiter(redis.Object);

        var activePenalty = await limiter.GetActivePenaltyAsync("user:42");

        activePenalty.Should().BeNull();
    }

    [Fact]
    public async Task RecordViolation_FailClosed_ReportsBackendUnavailable()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);

        var act = () => limiter.RecordRateLimitViolationAsync(
            "user:42",
            violationThreshold: 5,
            decayWindow: TimeSpan.FromMinutes(15),
            basePenalty: TimeSpan.FromSeconds(30),
            maxPenalty: TimeSpan.FromMinutes(15));

        await act.Should().ThrowAsync<RateLimitBackendUnavailableException>();
    }

    [Fact]
    public async Task RecordViolation_DefaultFailureModeReturnsNoPenaltyWhenRedisFails()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailOpenLimiter(redis.Object);

        var penalty = await limiter.RecordRateLimitViolationAsync(
            "user:42",
            violationThreshold: 5,
            decayWindow: TimeSpan.FromMinutes(15),
            basePenalty: TimeSpan.FromSeconds(30),
            maxPenalty: TimeSpan.FromMinutes(15));

        penalty.Should().BeNull();
    }

    [Fact]
    public async Task FixedWindow_CancellationPropagatesInsteadOfApplyingFailureMode()
    {
        var (redis, _) = CreateFailingRedis();
        var limiter = CreateFailClosedLimiter(redis.Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => limiter.IsAllowedFixedWindowAsync(
            "user:42",
            10,
            TimeSpan.FromMinutes(1),
            cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void RateLimitingOptions_RejectsUnknownRedisFailureMode()
    {
        var options = new RateLimitingOptions { RedisFailureMode = (RedisRateLimitFailureMode)99 };

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RedisFailureMode*");
    }

    private static RedisDistributedRateLimiter CreateFailClosedLimiter(IConnectionMultiplexer redis)
    {
        return new RedisDistributedRateLimiter(
            redis,
            NullLogger<RedisDistributedRateLimiter>.Instance,
            new RateLimitingOptions { RedisFailureMode = RedisRateLimitFailureMode.FailClosed });
    }

    private static RedisDistributedRateLimiter CreateFailOpenLimiter(IConnectionMultiplexer redis)
    {
        return new RedisDistributedRateLimiter(
            redis,
            NullLogger<RedisDistributedRateLimiter>.Instance,
            new RateLimitingOptions { RedisFailureMode = RedisRateLimitFailureMode.FailOpen });
    }

    private static (Mock<IConnectionMultiplexer> Redis, Mock<IDatabase> Database) CreateFailingRedis()
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("Redis is unavailable for this test."));

        var redis = new Mock<IConnectionMultiplexer>();
        redis
            .Setup(connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(database.Object);

        return (redis, database);
    }
}
