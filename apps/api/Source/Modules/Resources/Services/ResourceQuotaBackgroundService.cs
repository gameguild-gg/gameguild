using GameGuild.Database;
using GameGuild.Database;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Services;

/// <summary>
/// Implementation of background service for automated resource quota operations
/// </summary>
public class ResourceQuotaBackgroundService : IResourceQuotaBackgroundService
{
    private readonly ApplicationDbContext _context;
    private readonly IOutboxEventPublisher _eventPublisher;
    private readonly ILogger<ResourceQuotaBackgroundService> _logger;

    public ResourceQuotaBackgroundService(
        ApplicationDbContext context,
        IOutboxEventPublisher eventPublisher,
        ILogger<ResourceQuotaBackgroundService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task ResetDueQuotasAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting automated quota reset check");

        var quotas = await _context.Set<ResourceQuota>()
            .Where(q => q.IsActive)
            .Where(q => q.Period != ResourceQuotaPeriod.Never)
            .ToListAsync(cancellationToken);

        var resetCount = 0;

        foreach (var quota in quotas)
        {
            if (quota.ShouldReset())
            {
                var oldUsage = quota.CurrentUsage;
                quota.Reset();

                // Publish quota reset event to outbox
                await _eventPublisher.PublishAsync(new QuotaResetEvent
                {
                    QuotaId = quota.Id,
                    TenantId = quota.TenantId,
                    UsageType = quota.Type,
                    PreviousUsage = oldUsage,
                    ResetAt = DateTime.UtcNow,
                    Period = quota.Period
                }, cancellationToken);

                resetCount++;
            }
        }

        if (resetCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reset {Count} quotas automatically", resetCount);
        }
        else
        {
            _logger.LogInformation("No quotas due for reset");
        }
    }

    public async Task ArchiveOldUsageRecordsAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting automated usage record archival (retention: {Days} days)", retentionDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var count = await _context.Set<ResourceUsageRecord>()
            .Where(r => r.RecordedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Archived {Count} old usage records", count);

        // Publish archival event
        await _eventPublisher.PublishAsync(new UsageRecordsArchivedEvent
        {
            ArchivedCount = count,
            CutoffDate = cutoffDate,
            ArchivedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task CheckQuotaThresholdsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting quota threshold check");

        var quotas = await _context.Set<ResourceQuota>()
            .Where(q => q.IsActive && q.NotificationsEnabled)
            .Where(q => q.HardLimit.HasValue && q.HardLimit > 0)
            .ToListAsync(cancellationToken);

        foreach (var quota in quotas)
        {
            var percentage = quota.GetUsagePercentage();
            var thresholds = ParseThresholds(quota.NotificationThresholds);

            foreach (var threshold in thresholds)
            {
                if (percentage >= threshold && !HasRecentNotification(quota.Id, threshold))
                {
                    // Publish threshold exceeded event
                    await _eventPublisher.PublishAsync(new QuotaThresholdExceededEvent
                    {
                        QuotaId = quota.Id,
                        TenantId = quota.TenantId,
                        UsageType = quota.Type,
                        CurrentUsage = quota.CurrentUsage,
                        HardLimit = quota.HardLimit!.Value,
                        Percentage = percentage,
                        Threshold = threshold,
                        DetectedAt = DateTime.UtcNow
                    }, cancellationToken);

                    _logger.LogWarning(
                        "Quota threshold exceeded: Tenant {TenantId}, Type {Type}, Usage {Percentage}% (threshold {Threshold}%)",
                        quota.TenantId, quota.Type, percentage, threshold);
                }
            }
        }
    }

    private static List<double> ParseThresholds(string? thresholds)
    {
        if (string.IsNullOrEmpty(thresholds))
            return new List<double> { 75, 90, 100 };

        return thresholds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => double.TryParse(t.Trim(), out var value) ? value : 0)
            .Where(t => t > 0)
            .OrderBy(t => t)
            .ToList();
    }

    private bool HasRecentNotification(Guid quotaId, double threshold)
    {
        // Check if notification was sent in last hour to avoid spam
        // Implementation would query notification history table
        return false; // Simplified for now
    }
}
