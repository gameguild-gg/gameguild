using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Default <see cref="IPermissionExpirationAnalyticsService"/> (issue #331 "expiration
///     analytics and reporting"). Computes dashboard buckets from the live grant population
///     and reconstructs the daily expiration funnel from the permission audit log — automatic
///     expirations (<see cref="PermissionOperationType.Expire"/>) and manual expiration
///     mutations (bulk set/extend/clear, identified by their old→new expiration audit values).
/// </summary>
/// <remarks>
///     Read-only: every query path uses <c>AsNoTracking</c> repository reads; no analytics
///     operation mutates grants, audit rows, or the tenant security version.
/// </remarks>
public sealed class PermissionExpirationAnalyticsService(
    ITenantPermissionRepository permissionRepository,
    IPermissionAuditLogRepository auditLogRepository,
    IOptions<PermissionExpirationOptions> expirationOptions,
    ILogger<PermissionExpirationAnalyticsService> logger) : IPermissionExpirationAnalyticsService
{
    private static readonly string[] BucketOrder =
    {
        "expired-pending-processing", "within-24-hours", "1-to-3-days", "3-to-7-days", "7-to-30-days", "beyond-30-days"
    };

    private TimeSpan AnalyticsWindow => expirationOptions.Value.UpcomingNotificationWindow;

    /// <inheritdoc />
    public async Task<PermissionExpirationSummary> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var grants = await permissionRepository
            .GetByTenantAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        var active = grants.Where(g => g.IsActive).ToList();
        var temporary = active.Where(g => g.ExpiresAt.HasValue).ToList();

        var counts = new Dictionary<string, int>();
        foreach (var bucket in BucketOrder)
        {
            counts[bucket] = 0;
        }

        foreach (var grant in temporary)
        {
            counts[ClassifyBucket(grant.ExpiresAt!.Value, now)]++;
        }

        var windowEnd = now + AnalyticsWindow;
        var expiringByPermission = temporary
            .Where(g => g.ExpiresAt!.Value <= windowEnd)
            .SelectMany(g => g.Permissions.Select(p => (Permission: p, ExpiresAt: g.ExpiresAt!.Value)))
            .GroupBy(entry => entry.Permission, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ExpiringPermissionBreakdown
            {
                Permission = group.Key,
                ExpiringCount = group.Count(),
                EarliestExpiration = group.Min(e => e.ExpiresAt)
            })
            .OrderBy(b => b.EarliestExpiration)
            .ThenBy(b => b.Permission, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var summary = new PermissionExpirationSummary
        {
            TenantId = tenantId,
            ComputedAt = now,
            TotalGrants = grants.Count,
            ActiveTemporary = temporary.Count,
            ActivePermanent = active.Count - temporary.Count,
            Inactive = grants.Count - active.Count,
            Buckets = counts.Select(pair => new PermissionExpirationBucket
                {
                    Bucket = pair.Key,
                    Label = LabelFor(pair.Key),
                    Count = pair.Value
                })
                .Concat(new[]
                {
                    new PermissionExpirationBucket { Bucket = "permanent", Label = "Active grants without expiration", Count = active.Count - temporary.Count },
                    new PermissionExpirationBucket { Bucket = "inactive", Label = "Deactivated grants", Count = grants.Count - active.Count }
                })
                .ToList(),
            ExpiringByPermission = expiringByPermission
        };

        logger.LogDebug(
            "Computed expiration summary for tenant {TenantId}: {Total} grants, {ExpiringSoon} expiring within {Window}.",
            tenantId,
            summary.TotalGrants,
            expiringByPermission.Sum(b => b.ExpiringCount),
            AnalyticsWindow);

        return summary;
    }

    /// <inheritdoc />
    public async Task<List<PermissionExpirationTrendPoint>> GetTrendsAsync(
        Guid tenantId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate)
        {
            throw new ArgumentException("The trend period end must not precede its start.", nameof(toDate));
        }

        var logs = await auditLogRepository
            .GetByDateRangeAsync(fromDate.Date, toDate.Date.AddDays(1).AddTicks(-1), tenantId, cancellationToken)
            .ConfigureAwait(false);

        var expirations = logs
            .Where(l => l.OperationType == PermissionOperationType.Expire)
            .GroupBy(l => l.Timestamp.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var mutations = logs
            .Where(l => l.OperationType == PermissionOperationType.Update
                && string.Equals(l.ResourceType, "TenantPermission", StringComparison.OrdinalIgnoreCase)
                && IsExpirationValue(l.OldValue)
                && IsExpirationValue(l.NewValue))
            .GroupBy(l => l.Timestamp.Date)
            .ToDictionary(
                g => g.Key,
                g => new { Mutations = g.Count(), Cleared = g.Count(l => l.NewValue == "permanent") });

        var days = Enumerable
            .Range(0, int.Clamp((toDate.Date - fromDate.Date).Days, 0, 366) + 1)
            .Select(offset => fromDate.Date.AddDays(offset));

        return days
            .Select(day => new PermissionExpirationTrendPoint
            {
                Date = day,
                ExpirationsProcessed = expirations.GetValueOrDefault(day),
                ExpirationMutations = mutations.TryGetValue(day, out var mutation) ? mutation.Mutations : 0,
                ExpirationsCleared = mutations.TryGetValue(day, out var cleared) ? cleared.Cleared : 0
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<PermissionExpirationReport> GetReportAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        if (periodEnd < periodStart)
        {
            throw new ArgumentException("The report period end must not precede its start.", nameof(periodEnd));
        }

        var summary = await GetSummaryAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var trends = await GetTrendsAsync(tenantId, periodStart, periodEnd, cancellationToken).ConfigureAwait(false);

        var flags = new List<string>();
        var pendingProcessing = summary.Buckets.First(b => b.Bucket == "expired-pending-processing").Count;
        if (pendingProcessing > 0)
        {
            flags.Add(
                $"{pendingProcessing} expired grant(s) are still pending background processing; trigger permissions:process-expired or verify the worker is enabled.");
        }

        if (summary.ActivePermanent > 0 && expirationOptions.Value.GlobalRule.RequireExpiration == true)
        {
            flags.Add(
                $"{summary.ActivePermanent} permanent grant(s) predate the RequireExpiration rule; review them for re-issuance with an expiration.");
        }

        var report = new PermissionExpirationReport
        {
            TenantId = tenantId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Summary = summary,
            Trends = trends,
            AttentionFlags = flags
        };

        logger.LogInformation(
            "Generated expiration report for tenant {TenantId}: {Expirations} expirations, {Mutations} mutations, {Flags} attention flag(s).",
            tenantId,
            report.TotalExpirationsProcessed,
            report.TotalExpirationMutations,
            flags.Count);

        return report;
    }

    private static string ClassifyBucket(DateTime expiresAt, DateTime now)
    {
        var remaining = expiresAt - now;
        if (remaining <= TimeSpan.Zero) { return "expired-pending-processing"; }
        if (remaining <= TimeSpan.FromHours(24)) { return "within-24-hours"; }
        if (remaining <= TimeSpan.FromDays(3)) { return "1-to-3-days"; }
        if (remaining <= TimeSpan.FromDays(7)) { return "3-to-7-days"; }
        if (remaining <= TimeSpan.FromDays(30)) { return "7-to-30-days"; }
        return "beyond-30-days";
    }

    private static string LabelFor(string bucket) => bucket switch
    {
        "expired-pending-processing" => "Expired, pending background processing",
        "within-24-hours" => "Expiring within 24 hours",
        "1-to-3-days" => "Expiring in 1 to 3 days",
        "3-to-7-days" => "Expiring in 3 to 7 days",
        "7-to-30-days" => "Expiring in 7 to 30 days",
        "beyond-30-days" => "Expiring beyond 30 days",
        _ => bucket
    };

    /// <summary>
    ///     Recognizes the audit values written by <c>PermissionExpirationService</c>'s bulk
    ///     mutations: either an ISO-8601 round-trip timestamp or the literal "permanent".
    ///     Grant/revoke audits carry permission-string payloads and do not match.
    /// </summary>
    private static bool IsExpirationValue(string? value) =>
        value == "permanent"
        || (value is not null
            && DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out _));
}
