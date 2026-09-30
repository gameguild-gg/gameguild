using System.Text.Json;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>
///     Receives permission-cache invalidation events and clears the local instance cache.
/// </summary>
public sealed class RedisPermissionCacheInvalidationSubscriber : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuthorizationCacheOptions _options;
    private readonly ILogger<RedisPermissionCacheInvalidationSubscriber> _logger;

    /// <summary>
    ///     Creates a subscriber that dispatches received events through a scoped invalidation service.
    /// </summary>
    public RedisPermissionCacheInvalidationSubscriber(
        IConnectionMultiplexer connection,
        IServiceScopeFactory scopeFactory,
        IOptions<AuthorizationCacheOptions> options,
        ILogger<RedisPermissionCacheInvalidationSubscriber> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.UseDistributedCache || !_options.UsePubSubInvalidation)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.InvalidationChannelName))
        {
            _logger.LogError("Authorization cache invalidation channel name is empty; Redis subscription is disabled.");
            return;
        }

        var channel = RedisChannel.Literal(_options.InvalidationChannelName);
        var subscriber = _connection.GetSubscriber();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await subscriber.SubscribeAsync(channel, (_, message) => QueueMessage(message)).ConfigureAwait(false);

                _logger.LogInformation(
                    "Subscribed to permission cache invalidations on Redis channel {Channel}",
                    _options.InvalidationChannelName);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Redis permission cache invalidation subscription failed; retrying in five seconds");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        if (!_options.UseDistributedCache || !_options.UsePubSubInvalidation ||
            string.IsNullOrWhiteSpace(_options.InvalidationChannelName))
        {
            return;
        }

        try
        {
            await _connection.GetSubscriber()
                .UnsubscribeAsync(RedisChannel.Literal(_options.InvalidationChannelName))
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception,
                "Could not unsubscribe from permission cache invalidations on shutdown");
        }
    }

    private async Task HandleMessageAsync(string payload)
    {
        try
        {
            var invalidationEvent = JsonSerializer.Deserialize<CacheInvalidationEvent>(payload, JsonOptions);
            if (invalidationEvent is null || !Enum.IsDefined(invalidationEvent.Type) ||
                (invalidationEvent.Type != CacheInvalidationType.Global && invalidationEvent.TenantId == Guid.Empty) ||
                (invalidationEvent.Type == CacheInvalidationType.Batch &&
                 !CacheInvalidationService.IsValidBatchTargets(invalidationEvent.Targets)))
            {
                _logger.LogWarning("Ignoring malformed permission cache invalidation event");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var invalidationService = scope.ServiceProvider.GetRequiredService<ICacheInvalidationService>();
            invalidationService.HandleInvalidationEvent(invalidationEvent);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Ignoring unreadable permission cache invalidation event");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle permission cache invalidation event");
        }
    }

    private void QueueMessage(RedisValue message)
    {
        var payload = message.ToString();
        if (!string.IsNullOrWhiteSpace(payload))
        {
            _ = HandleMessageAsync(payload);
        }
    }
}
