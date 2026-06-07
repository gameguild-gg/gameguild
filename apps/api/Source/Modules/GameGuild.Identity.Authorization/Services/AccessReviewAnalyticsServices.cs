using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for managing Access Review campaigns and items
/// </summary>
public class AccessReviewService(
    IAccessReviewCampaignRepository campaignRepository,
    IAccessReviewItemRepository itemRepository,
    ILogger<AccessReviewService> logger,
    IPublisher? publisher = null
) : IAccessReviewService
{
    private readonly IAccessReviewCampaignRepository _campaignRepository =
        campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));

    private readonly IAccessReviewItemRepository _itemRepository =
        itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));

    private readonly ILogger<AccessReviewService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPublisher? _publisher = publisher;

    public async Task<AccessReviewCampaign> CreateCampaignAsync(
        AccessReviewCampaign campaign,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _campaignRepository.CreateAsync(campaign, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created access review campaign {CampaignId}: {Name}",
            result.Id,
            campaign.Name
        );

        return result;
    }

    public async Task<AccessReviewCampaign> UpdateCampaignAsync(
        AccessReviewCampaign campaign,
        CancellationToken cancellationToken = default
    )
    {
        await _campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated access review campaign {CampaignId}", campaign.Id);

        return campaign;
    }

    public async Task<bool> StartCampaignAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default
    )
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);

        if (campaign == null) return false;

        campaign.Start();
        await _campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Started access review campaign {CampaignId}", campaignId);

        return true;
    }

    public async Task<bool> CompleteCampaignAsync(
        Guid campaignId,
        Guid completedBy,
        CancellationToken cancellationToken = default
    )
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);

        if (campaign == null) return false;

        campaign.Complete(completedBy);
        await _campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Completed access review campaign {CampaignId} by {CompletedBy}", campaignId, completedBy);

        return true;
    }

    public async Task<bool> CancelCampaignAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default
    )
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);

        if (campaign == null) return false;

        campaign.Cancel();
        await _campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cancelled access review campaign {CampaignId}", campaignId);

        return true;
    }

    public async Task<AccessReviewCampaign?> GetCampaignByIdAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default
    ) => await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);

    public async Task<List<AccessReviewCampaign>> GetActiveCampaignsAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _campaignRepository.GetActiveCampaignsAsync(tenantId, cancellationToken);

    public async Task<List<AccessReviewItem>> GetPendingItemsForReviewerAsync(
        Guid reviewerId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _itemRepository.GetPendingByReviewerAsync(reviewerId, cancellationToken);

    public async Task<AccessReviewItem> ApproveItemAsync(
        Guid itemId,
        string? reason = null,
        string? notes = null,
        CancellationToken cancellationToken = default
    )
    {
        var item = await _itemRepository.GetByIdAsync(itemId, cancellationToken).ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"Review item {itemId} not found");

        item.Approve(reason, notes);
        await _itemRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Approved review item {ItemId}", itemId);

        return item;
    }

    public async Task<AccessReviewItem> RevokeItemAsync(
        Guid itemId,
        string reason,
        string? notes = null,
        CancellationToken cancellationToken = default
    )
    {
        var item = await _itemRepository.GetByIdAsync(itemId, cancellationToken).ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"Review item {itemId} not found");

        item.Revoke(reason, notes);
        await _itemRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Revoked review item {ItemId}", itemId);

        return item;
    }

    public async Task<int> SendRemindersAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default
    )
    {
        var items = await _itemRepository.GetByCampaignAsync(campaignId, cancellationToken).ConfigureAwait(false);
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);

        if (campaign == null) return 0;

        var remindersSent = 0;

        foreach (var item in items.Where(i => i.NeedsReminder(campaign.ReminderFrequencyDays)))
        {
            item.RecordReminderSent();
            await _itemRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            remindersSent++;

            if (_publisher is not null)
            {
                await _publisher.Publish(
                    new AccessReviewReminderNotification(campaignId, item.Id, item.ReviewerId),
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Sent {Count} reminders for campaign {CampaignId}", remindersSent, campaignId);

        return remindersSent;
    }

    public async Task<int> ProcessExpiredCampaignsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var pendingCampaigns = await _campaignRepository.GetPendingCampaignsAsync(cancellationToken).ConfigureAwait(false);
        var expiredCount = 0;

        foreach (var campaign in pendingCampaigns.Where(c => c.IsExpired()))
        {
            campaign.MarkExpired();
            await _campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);
            expiredCount++;
        }

        _logger.LogInformation("Marked {Count} campaigns as expired", expiredCount);

        return expiredCount;
    }
}

/// <summary>
///     Service for permission analytics and reporting
/// </summary>
public class PermissionAnalyticsService(
    IPermissionAuditLogRepository auditLogRepository,
    ILogger<PermissionAnalyticsService> logger
) : IPermissionAnalyticsService
{
    private readonly IPermissionAuditLogRepository _auditLogRepository =
        auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));

    private readonly ILogger<PermissionAnalyticsService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<List<PermissionUsageMetrics>> GetPermissionUsageAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var logs = await _auditLogRepository.GetByDateRangeAsync(
            fromDate ?? SystemClock.UtcNow.AddMonths(-1),
            toDate ?? SystemClock.UtcNow,
            tenantId,
            cancellationToken
        ).ConfigureAwait(false);

        return logs
            .Where(l => l.PermissionType != null)
            .GroupBy(l => l.PermissionType!)
            .Select(g => new PermissionUsageMetrics
            {
                Permission = g.Key,
                UsageCount = g.Count(),
                UniqueUsers = g.Select(l => l.UserId).Distinct().Count(),
                LastUsed = g.Max(l => l.Timestamp)
            })
            .OrderByDescending(m => m.UsageCount)
            .ToList();
    }

    public async Task<List<UserActivitySummary>> GetUserActivityAsync(
        Guid? tenantId,
        int top = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var logs = await _auditLogRepository.GetByDateRangeAsync(
            fromDate ?? SystemClock.UtcNow.AddMonths(-1),
            toDate ?? SystemClock.UtcNow,
            tenantId,
            cancellationToken
        ).ConfigureAwait(false);

        return logs
            .Where(l => l.UserId.HasValue)
            .GroupBy(l => l.UserId!.Value)
            .Select(g => new UserActivitySummary
            {
                UserId = g.Key,
                TotalActions = g.Count(),
                PermissionChanges = g.Count(l => l.OperationType is PermissionOperationType.Grant or PermissionOperationType.Revoke),
                LastActivity = g.Max(l => l.Timestamp)
            })
            .OrderByDescending(s => s.TotalActions)
            .Take(top)
            .ToList();
    }

    public async Task<List<ResourceAccessPattern>> GetResourceAccessPatternsAsync(
        Guid? tenantId,
        int top = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var logs = await _auditLogRepository.GetByDateRangeAsync(
            fromDate ?? SystemClock.UtcNow.AddMonths(-1),
            toDate ?? SystemClock.UtcNow,
            tenantId,
            cancellationToken
        ).ConfigureAwait(false);

        return logs
            .Where(l => l.ResourceId.HasValue && l.ResourceType != null)
            .GroupBy(l => new { l.ResourceId, l.ResourceType })
            .Select(g => new ResourceAccessPattern
            {
                ResourceId = g.Key.ResourceId!.Value,
                ResourceType = g.Key.ResourceType!,
                AccessCount = g.Count(),
                UniqueUsers = g.Select(l => l.UserId).Distinct().Count()
            })
            .OrderByDescending(p => p.AccessCount)
            .Take(top)
            .ToList();
    }

    public async Task<List<PermissionTrend>> GetPermissionTrendsAsync(
        Guid? tenantId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default
    )
    {
        var logs = await _auditLogRepository.GetByDateRangeAsync(
            fromDate,
            toDate,
            tenantId,
            cancellationToken
        ).ConfigureAwait(false);

        var dailyTrends = logs
            .GroupBy(l => l.Timestamp.Date)
            .Select(g => new PermissionTrend
            {
                Date = g.Key,
                Grants = g.Count(l => l.OperationType == PermissionOperationType.Grant),
                Revokes = g.Count(l => l.OperationType == PermissionOperationType.Revoke),
            })
            .OrderBy(t => t.Date)
            .ToList();

        var activePermissions = 0;
        foreach (var trend in dailyTrends)
        {
            activePermissions += trend.Grants - trend.Revokes;
            trend.ActivePermissions = activePermissions;
        }

        return dailyTrends;
    }

    public async Task<List<PermissionAnomaly>> DetectAnomaliesAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Detecting permission anomalies for tenant {TenantId} from {FromDate}",
            tenantId,
            fromDate
        );

        var logs = await _auditLogRepository.GetByDateRangeAsync(
            fromDate ?? SystemClock.UtcNow.AddDays(-7),
            SystemClock.UtcNow,
            tenantId,
            cancellationToken
        ).ConfigureAwait(false);

        var anomalies = new List<PermissionAnomaly>();

        // Detect unusual patterns: excessive grants/revokes
        var userGrantCounts = logs
            .Where(l => l.OperationType == PermissionOperationType.Grant)
            .GroupBy(l => l.PerformedBy)
            .Where(g => g.Count() > 50) // Threshold
            .ToList();

        foreach (var group in userGrantCounts)
        {
            anomalies.Add(new PermissionAnomaly
            {
                UserId = group.Key,
                AnomalyType = "ExcessiveGrants",
                Description = $"User performed {group.Count()} permission grants in the period",
                DetectedAt = SystemClock.UtcNow,
                Severity = ImpactSeverity.Medium
            });
        }

        return anomalies;
    }

    public async Task<PermissionAnalyticsReport> GenerateReportAsync(
        Guid? tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Generating permission analytics report for tenant {TenantId} from {PeriodStart} to {PeriodEnd}",
            tenantId,
            periodStart,
            periodEnd
        );

        var topPermissions = await GetPermissionUsageAsync(tenantId, periodStart, periodEnd, cancellationToken).ConfigureAwait(false);
        var topUsers = await GetUserActivityAsync(tenantId, 10, periodStart, periodEnd, cancellationToken).ConfigureAwait(false);
        var anomalies = await DetectAnomaliesAsync(tenantId, periodStart, cancellationToken).ConfigureAwait(false);

        var logs = await _auditLogRepository.GetByDateRangeAsync(periodStart, periodEnd, tenantId, cancellationToken).ConfigureAwait(false);

        return new PermissionAnalyticsReport
        {
            TenantId = tenantId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TopPermissions = topPermissions.Take(10).ToList(),
            TopUsers = topUsers,
            Anomalies = anomalies,
            TotalGrants = logs.Count(l => l.OperationType == PermissionOperationType.Grant),
            TotalRevokes = logs.Count(l => l.OperationType == PermissionOperationType.Revoke),
            ActiveUsers = logs.Select(l => l.UserId).Distinct().Count()
        };
    }
}
