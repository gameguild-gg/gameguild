namespace GameGuild.Modules.Common.Outbox;

/// <summary>
/// Background service that processes outbox messages
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;
    private readonly int _maxRetries = 3;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var messages = await outboxService.GetPendingMessagesAsync(_batchSize, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                // Check retry limit
                if (message.RetryCount >= _maxRetries)
                {
                    _logger.LogWarning(
                        "Message {MessageId} exceeded max retries ({MaxRetries}), marking as failed",
                        message.Id, _maxRetries);

                    await outboxService.MarkAsFailedAsync(
                        message.Id,
                        $"Exceeded maximum retry attempts ({_maxRetries})",
                        cancellationToken);

                    continue;
                }

                // Process message (publish to message bus, call external service, etc.)
                await ProcessMessageAsync(message, scope.ServiceProvider, cancellationToken);

                // Mark as processed
                await outboxService.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogDebug("Successfully processed message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", message.Id);

                await outboxService.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // TODO: Implement actual message processing logic
        // This is where you would:
        // 1. Deserialize the payload
        // 2. Publish to message bus (RabbitMQ, Azure Service Bus, etc.)
        // 3. Call external services
        // 4. Raise domain events

        _logger.LogDebug(
            "Processing message {MessageId} of type {MessageType}",
            message.Id, message.MessageType);

        // Simulate processing
        await Task.CompletedTask;
    }
}
