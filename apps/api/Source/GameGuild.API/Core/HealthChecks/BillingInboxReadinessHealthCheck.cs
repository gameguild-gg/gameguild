using GameGuild.API.Database;
using GameGuild.Commerce.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GameGuild.API.HealthChecks;

internal sealed class BillingInboxReadinessHealthCheck(
    ApplicationDbContext dbContext,
    IOptions<BillingConfiguration> billingOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookOptions = billingOptions.Value.Webhook;
            var checkedAt = SystemClock.UtcNow;
            var staleBefore = checkedAt.AddSeconds(-webhookOptions.ProcessingTimeoutSeconds);
            var inboxState = await dbContext.Set<BillingWebhookEvent>()
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(events => new
                {
                    PendingEvents = events.Count(webhookEvent =>
                        !webhookEvent.IsProcessed && !webhookEvent.IsFailed),
                    StaleEvents = events.Count(webhookEvent =>
                        !webhookEvent.IsProcessed && !webhookEvent.IsFailed && webhookEvent.CreatedAt <= staleBefore),
                    FailedEvents = events.Count(webhookEvent =>
                        !webhookEvent.IsProcessed && webhookEvent.IsFailed),
                    ExhaustedEvents = events.Count(webhookEvent =>
                        !webhookEvent.IsProcessed &&
                        webhookEvent.ProcessingAttempts >= webhookOptions.MaxRetryAttempts),
                    LegacyEvents = events.Count(webhookEvent => webhookEvent.ProviderEnvironment == null),
                    OldestPendingCreatedAt = events
                        .Where(webhookEvent => !webhookEvent.IsProcessed && !webhookEvent.IsFailed)
                        .Select(webhookEvent => (DateTime?)webhookEvent.CreatedAt)
                        .Min()
                })
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var pendingEvents = inboxState?.PendingEvents ?? 0;
            var staleEvents = inboxState?.StaleEvents ?? 0;
            var failedEvents = inboxState?.FailedEvents ?? 0;
            var exhaustedEvents = inboxState?.ExhaustedEvents ?? 0;
            var legacyEvents = inboxState?.LegacyEvents ?? 0;
            var oldestPendingAgeSeconds = inboxState?.OldestPendingCreatedAt is { } oldestPendingCreatedAt
                ? Math.Max(0L, (long)(checkedAt - oldestPendingCreatedAt).TotalSeconds)
                : 0L;
            var data = new Dictionary<string, object>
            {
                ["pendingEvents"] = pendingEvents,
                ["staleEvents"] = staleEvents,
                ["failedEvents"] = failedEvents,
                ["exhaustedEvents"] = exhaustedEvents,
                ["legacyEvents"] = legacyEvents,
                ["oldestPendingAgeSeconds"] = oldestPendingAgeSeconds
            };

            return staleEvents == 0 && failedEvents == 0 && exhaustedEvents == 0 && legacyEvents == 0
                ? HealthCheckResult.Healthy("Billing webhook inbox is ready.", data)
                : HealthCheckResult.Degraded("Billing webhook inbox requires attention.", data: data);
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Billing webhook inbox readiness check failed.");
        }
    }
}
