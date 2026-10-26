using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GameGuild.API.Eventing;

public sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var drainBacklog = false;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatched = await scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>()
                    .DispatchPendingAsync(stoppingToken)
                    .ConfigureAwait(false);

                // Drain a backlog without allowing retention to monopolize the dispatcher.
                // Each batch gets a fresh context, bounding tracking memory for large queues.
                var cleanupTime = Stopwatch.StartNew();
                var retired = 0;
                for (var batch = 0; batch < 8 && cleanupTime.Elapsed < TimeSpan.FromMilliseconds(500); batch++)
                {
                    await using var cleanupScope = scopeFactory.CreateAsyncScope();
                    var count = await cleanupScope.ServiceProvider.GetRequiredService<OutboxCleanupService>()
                        .CleanupCompletedAsync(stoppingToken).ConfigureAwait(false);
                    retired += count;
                    if (count < OutboxCleanupService.BatchSize)
                    {
                        break;
                    }
                }

                if (retired > 0)
                {
                    logger.LogInformation("Retired {EventCount} completed integration-event payloads", retired);
                }
                drainBacklog = dispatched == OutboxDispatcher.BatchSize;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Durable integration-event dispatch cycle failed");
            }

            if (!drainBacklog)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
