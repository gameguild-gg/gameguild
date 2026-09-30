using System.Text.Json;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>
///     Publishes permission-cache invalidation events to Redis Pub/Sub.
/// </summary>
public interface ICacheInvalidationPublisher
{
    /// <summary>
    ///     Publishes an invalidation event to the configured Redis channel.
    /// </summary>
    Task PublishAsync(CacheInvalidationEvent invalidationEvent);

    /// <summary>
    ///     Publishes an invalidation event with cancellation support.
    /// </summary>
    Task PublishAsync(CacheInvalidationEvent invalidationEvent, CancellationToken cancellationToken);
}

/// <summary>
///     Redis Pub/Sub publisher for permission-cache invalidation.
/// </summary>
public sealed class RedisPermissionCacheInvalidationPublisher : ICacheInvalidationPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connection;
    private readonly AuthorizationCacheOptions _options;
    private readonly ILogger<RedisPermissionCacheInvalidationPublisher> _logger;

    /// <summary>
    ///     Creates a publisher backed by the shared Redis multiplexer.
    /// </summary>
    public RedisPermissionCacheInvalidationPublisher(
        IConnectionMultiplexer connection,
        IOptions<AuthorizationCacheOptions> options,
        ILogger<RedisPermissionCacheInvalidationPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PublishAsync(CacheInvalidationEvent invalidationEvent)
    {
        return PublishAsync(invalidationEvent, CancellationToken.None);
    }

    public async Task PublishAsync(
        CacheInvalidationEvent invalidationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invalidationEvent);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_options.InvalidationChannelName))
        {
            throw new InvalidOperationException("Authorization cache invalidation channel name is required.");
        }

        var payload = JsonSerializer.Serialize(invalidationEvent, JsonOptions);

        try
        {
            await _connection.GetSubscriber()
                .PublishAsync(RedisChannel.Literal(_options.InvalidationChannelName), payload)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // The tenant security version is advanced before publication, so cache keys
            // derived from the old version are bypassed even while Redis reconnects.
            _logger.LogError(exception,
                "Failed to publish permission cache invalidation for tenant {TenantId} to channel {Channel}",
                invalidationEvent.TenantId,
                _options.InvalidationChannelName);
        }
    }
}
