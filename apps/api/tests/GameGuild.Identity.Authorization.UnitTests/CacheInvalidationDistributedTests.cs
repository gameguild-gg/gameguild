using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Text.Json;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class CacheInvalidationDistributedTests
{
    [Fact]
    public void AddAuthorizationRedisCache_RegistersPublisherAndPerInstanceSubscriber()
    {
        var services = new ServiceCollection();
        services.AddAuthorizationRedisCache("localhost:6379", "unit-auth:");

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICacheInvalidationPublisher));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(RedisPermissionCacheInvalidationSubscriber));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationCacheOptions>>().Value;
        options.UseDistributedCache.Should().BeTrue();
        options.RedisConnectionString.Should().Be("localhost:6379");
        options.RedisInstanceName.Should().Be("unit-auth:");
    }

    [Fact]
    public async Task RedisPublisher_SendsInvalidationOnConfiguredChannel()
    {
        var subscriber = new Mock<ISubscriber>();
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(multiplexer => multiplexer.GetSubscriber(It.IsAny<object?>()))
            .Returns(subscriber.Object);
        var publishedChannels = new List<RedisChannel>();
        var publishedMessages = new List<RedisValue>();
        subscriber
            .Setup(redisSubscriber => redisSubscriber.PublishAsync(
                Capture.In(publishedChannels), Capture.In(publishedMessages), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        var options = Options.Create(new AuthorizationCacheOptions
        {
            InvalidationChannelName = "gameguild:auth:invalidate"
        });
        var service = new RedisPermissionCacheInvalidationPublisher(
            connection.Object,
            options,
            NullLogger<RedisPermissionCacheInvalidationPublisher>.Instance);
        var invalidationEvent = new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Policy,
            TenantId = Guid.NewGuid(),
            PolicyName = "documents.read",
            OriginInstanceId = "instance-a"
        };

        await service.PublishAsync(invalidationEvent);

        publishedChannels.Should().ContainSingle()
            .Which.ToString().Should().Be("gameguild:auth:invalidate");
        publishedMessages.Should().ContainSingle();
        JsonSerializer.Deserialize<CacheInvalidationEvent>(
                publishedMessages.Single().ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .Should().BeEquivalentTo(invalidationEvent);
    }

    [Fact]
    public async Task RedisPublisher_WhenRedisIsUnavailable_DoesNotFailTheInvalidationCaller()
    {
        var subscriber = new Mock<ISubscriber>();
        subscriber
            .Setup(redisSubscriber => redisSubscriber.PublishAsync(
                It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new TimeoutException("Redis is unavailable."));
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(multiplexer => multiplexer.GetSubscriber(It.IsAny<object?>()))
            .Returns(subscriber.Object);
        var service = new RedisPermissionCacheInvalidationPublisher(
            connection.Object,
            Options.Create(new AuthorizationCacheOptions()),
            NullLogger<RedisPermissionCacheInvalidationPublisher>.Instance);

        var act = () => service.PublishAsync(new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Tenant,
            TenantId = Guid.NewGuid(),
            OriginInstanceId = "instance-a"
        });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RedisSubscriber_DispatchesInvalidationToScopedService()
    {
        Action<RedisChannel, RedisValue>? messageHandler = null;
        var subscriptionReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var redisSubscriber = new Mock<ISubscriber>();
        redisSubscriber
            .Setup(subscriber => subscriber.SubscribeAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>(
                (_, handler, _) =>
                {
                    messageHandler = handler;
                    subscriptionReady.TrySetResult();
                })
            .Returns(Task.CompletedTask);
        redisSubscriber
            .Setup(subscriber => subscriber.UnsubscribeAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(multiplexer => multiplexer.GetSubscriber(It.IsAny<object?>()))
            .Returns(redisSubscriber.Object);

        var expected = new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Global,
            TenantId = Guid.Empty,
            OriginInstanceId = "instance-b"
        };
        var handled = new TaskCompletionSource<CacheInvalidationEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var invalidationService = new Mock<ICacheInvalidationService>();
        invalidationService
            .Setup(service => service.HandleInvalidationEvent(It.IsAny<CacheInvalidationEvent>()))
            .Callback<CacheInvalidationEvent>(invalidationEvent => handled.TrySetResult(invalidationEvent));
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(provider => provider.GetService(typeof(ICacheInvalidationService)))
            .Returns(invalidationService.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(value => value.ServiceProvider).Returns(serviceProvider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(factory => factory.CreateScope()).Returns(scope.Object);

        var subscriberService = new RedisPermissionCacheInvalidationSubscriber(
            connection.Object,
            scopeFactory.Object,
            Options.Create(new AuthorizationCacheOptions
            {
                UseDistributedCache = true,
                UsePubSubInvalidation = true,
                InvalidationChannelName = "gameguild:auth:invalidate"
            }),
            NullLogger<RedisPermissionCacheInvalidationSubscriber>.Instance);

        await subscriberService.StartAsync(CancellationToken.None);
        await subscriptionReady.Task.WaitAsync(TimeSpan.FromSeconds(3));
        messageHandler.Should().NotBeNull();
        messageHandler!(
            RedisChannel.Literal("gameguild:auth:invalidate"),
            JsonSerializer.Serialize(expected, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var completed = await Task.WhenAny(handled.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(handled.Task, "the received event should reach the local invalidation service");
        (await handled.Task).Should().BeEquivalentTo(expected);
        await subscriberService.StopAsync(CancellationToken.None);
        redisSubscriber.Verify(subscriber => subscriber.UnsubscribeAsync(
            RedisChannel.Literal("gameguild:auth:invalidate"),
            It.IsAny<Action<RedisChannel, RedisValue>>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RedisSubscriber_RetriesAfterTransientSubscriptionFailure()
    {
        var attempts = 0;
        Action<RedisChannel, RedisValue>? messageHandler = null;
        var subscriptionReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var redisSubscriber = new Mock<ISubscriber>();
        redisSubscriber
            .Setup(subscriber => subscriber.SubscribeAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>(
                (_, handler, _) =>
                {
                    if (Interlocked.Increment(ref attempts) == 2)
                    {
                        messageHandler = handler;
                        subscriptionReady.TrySetResult();
                    }
                })
            .Returns(() => attempts == 1
                ? Task.FromException(new TimeoutException("Injected Redis subscription timeout."))
                : Task.CompletedTask);
        redisSubscriber
            .Setup(subscriber => subscriber.UnsubscribeAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);

        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(multiplexer => multiplexer.GetSubscriber(It.IsAny<object?>()))
            .Returns(redisSubscriber.Object);

        var expected = new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Resource,
            TenantId = Guid.NewGuid(),
            ResourceType = "Document",
            ResourceId = "document-1",
            OriginInstanceId = "instance-reconnected"
        };
        var handled = new TaskCompletionSource<CacheInvalidationEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var invalidationService = new Mock<ICacheInvalidationService>();
        invalidationService
            .Setup(service => service.HandleInvalidationEvent(It.IsAny<CacheInvalidationEvent>()))
            .Callback<CacheInvalidationEvent>(invalidationEvent => handled.TrySetResult(invalidationEvent));
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(provider => provider.GetService(typeof(ICacheInvalidationService)))
            .Returns(invalidationService.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(value => value.ServiceProvider).Returns(serviceProvider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(factory => factory.CreateScope()).Returns(scope.Object);

        var subscriberService = new RedisPermissionCacheInvalidationSubscriber(
            connection.Object,
            scopeFactory.Object,
            Options.Create(new AuthorizationCacheOptions
            {
                UseDistributedCache = true,
                UsePubSubInvalidation = true,
                InvalidationChannelName = "gameguild:auth:invalidate"
            }),
            NullLogger<RedisPermissionCacheInvalidationSubscriber>.Instance);

        await subscriberService.StartAsync(CancellationToken.None);
        try
        {
            await subscriptionReady.Task.WaitAsync(TimeSpan.FromSeconds(8));
            attempts.Should().Be(2, "the first subscribe call fails and the retry succeeds");
            messageHandler.Should().NotBeNull();
            messageHandler!(
                RedisChannel.Literal("gameguild:auth:invalidate"),
                JsonSerializer.Serialize(expected, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            var completed = await Task.WhenAny(handled.Task, Task.Delay(TimeSpan.FromSeconds(3)));
            completed.Should().Be(handled.Task,
                "the recovered subscription must resume dispatching Redis invalidation events");
            (await handled.Task).Should().BeEquivalentTo(expected);
        }
        finally
        {
            await subscriberService.StopAsync(CancellationToken.None);
        }

        redisSubscriber.Verify(subscriber => subscriber.SubscribeAsync(
            RedisChannel.Literal("gameguild:auth:invalidate"),
            It.IsAny<Action<RedisChannel, RedisValue>>(),
            It.IsAny<CommandFlags>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ResourceInvalidation_AdvancesVersionBeforeRedisPublishFailure()
    {
        var tenantId = Guid.NewGuid();
        var operations = new List<string>();
        var versionStore = new Mock<ITenantSecurityVersionStore>();
        versionStore
            .Setup(store => store.IncrementVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("tenant-version"))
            .ReturnsAsync(1);

        var hybridCache = new Mock<IHybridPermissionCache>();
        hybridCache
            .Setup(cache => cache.InvalidatePatternAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("local-and-l2-cleanup"))
            .Returns(Task.CompletedTask);

        var redisSubscriber = new Mock<ISubscriber>();
        redisSubscriber
            .Setup(subscriber => subscriber.PublishAsync(
                It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback(() => operations.Add("redis-publish"))
            .ThrowsAsync(new TimeoutException("Injected Redis publish timeout."));
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(multiplexer => multiplexer.GetSubscriber(It.IsAny<object?>()))
            .Returns(redisSubscriber.Object);

        var options = Options.Create(new AuthorizationCacheOptions
        {
            UseDistributedCache = true,
            UsePubSubInvalidation = true,
            InvalidationChannelName = "gameguild:auth:invalidate"
        });
        var publisher = new RedisPermissionCacheInvalidationPublisher(
            connection.Object,
            options,
            NullLogger<RedisPermissionCacheInvalidationPublisher>.Instance);
        var service = new CacheInvalidationService(
            new MemoryCache(new MemoryCacheOptions()),
            versionStore.Object,
            hybridCache.Object,
            new Mock<ICacheMetricsService>().Object,
            options,
            NullLogger<CacheInvalidationService>.Instance,
            publisher);

        await service.InvalidateResourceAsync(tenantId, "Document", "document-1");

        operations.Should().Equal("tenant-version", "local-and-l2-cleanup", "redis-publish");
        versionStore.Verify(
            store => store.IncrementVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
        redisSubscriber.Verify(subscriber => subscriber.PublishAsync(
            RedisChannel.Literal("gameguild:auth:invalidate"),
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Theory]
    [InlineData(CacheInvalidationType.Resource)]
    [InlineData(CacheInvalidationType.Policy)]
    public async Task ResourceAndPolicyInvalidation_AdvanceTenantSecurityVersion(
        CacheInvalidationType invalidationType)
    {
        var tenantId = Guid.NewGuid();
        var versionStore = new Mock<ITenantSecurityVersionStore>();
        versionStore
            .Setup(store => store.IncrementVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var hybridCache = new Mock<IHybridPermissionCache>();
        hybridCache
            .Setup(cache => cache.InvalidatePatternAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var metrics = new Mock<ICacheMetricsService>();
        var service = new CacheInvalidationService(
            new MemoryCache(new MemoryCacheOptions()),
            versionStore.Object,
            hybridCache.Object,
            metrics.Object,
            Options.Create(new AuthorizationCacheOptions()),
            NullLogger<CacheInvalidationService>.Instance);

        if (invalidationType == CacheInvalidationType.Resource)
        {
            await service.InvalidateResourceAsync(tenantId, "Document", "document-1");
        }
        else
        {
            await service.InvalidatePolicyAsync(tenantId, "documents.read");
        }

        versionStore.Verify(
            store => store.IncrementVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
