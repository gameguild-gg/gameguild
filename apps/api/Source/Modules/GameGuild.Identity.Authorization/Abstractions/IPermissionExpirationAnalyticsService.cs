namespace GameGuild.Identity.Authorization;

/// <summary>
///     A time-to-expiry bucket in the expiration dashboard (issue #331 "expiration analytics
/// and reporting"). Active temporary grants are counted into exactly one bucket by their
/// remaining lifetime; permanent and inactive grants are counted separately.
/// </summary>
public sealed record PermissionExpirationBucket
{
    /// <summary>
    ///     Gets the stable bucket identifier (e.g. <c>expired-pending-processing</c>,
    ///     <c>within-24-hours</c>, <c>1-to-3-days</c>, <c>3-to-7-days</c>, <c>7-to-30-days</c>,
    ///     <c>beyond-30-days</c>, <c>permanent</c>, <c>inactive</c>).
    /// </summary>
    public required string Bucket { get; init; }

    /// <summary>
    ///     Gets a human-readable label for the bucket.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    ///     Gets the number of grants in the bucket.
    /// </summary>
    public int Count { get; init; }
}

/// <summary>
///     Per-permission-type aggregation of expiring grants (top offenders for renewal planning).
/// </summary>
public sealed record ExpiringPermissionBreakdown
{
    /// <summary>
    ///     Gets the permission string.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    ///     Gets the number of active grants carrying the permission that expire within the
    ///     analytics window.
    /// </summary>
    public int ExpiringCount { get; init; }

    /// <summary>
    ///     Gets the earliest expiration among those grants.
    /// </summary>
    public DateTime EarliestExpiration { get; init; }
}

/// <summary>
///     Expiration dashboard summary for one tenant (issue #331): the full grant population
///     bucketed by time-to-expiry plus per-permission breakdown of upcoming expirations.
/// </summary>
public sealed record PermissionExpirationSummary
{
    /// <summary>
    ///     Gets the tenant the summary was computed for.
    /// </summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    ///     Gets the instant the summary was computed at.
    /// </summary>
    public required DateTime ComputedAt { get; init; }

    /// <summary>
    ///     Gets the total number of live (non-deleted) grants in the tenant.
    /// </summary>
    public int TotalGrants { get; init; }

    /// <summary>
    ///     Gets the number of active grants that carry an expiration (temporary grants).
    /// </summary>
    public int ActiveTemporary { get; init; }

    /// <summary>
    ///     Gets the number of active grants without an expiration (permanent grants).
    /// </summary>
    public int ActivePermanent { get; init; }

    /// <summary>
    ///     Gets the number of inactive (already deactivated) grants.
    /// </summary>
    public int Inactive { get; init; }

    /// <summary>
    ///     Gets the time-to-expiry buckets (always present, zero-count buckets included).
    /// </summary>
    public required IReadOnlyList<PermissionExpirationBucket> Buckets { get; init; }

    /// <summary>
    ///     Gets the per-permission breakdown of grants expiring within the analytics window,
    ///     most urgent first.
    /// </summary>
    public required IReadOnlyList<ExpiringPermissionBreakdown> ExpiringByPermission { get; init; }
}

/// <summary>
///     One day of the expiration funnel (issue #331 "expiry-funnel stats"): how many grants
///     were durably expired by the sweep, and how many manual expiration mutations were applied.
/// </summary>
public sealed record PermissionExpirationTrendPoint
{
    /// <summary>
    ///     Gets the UTC day the point covers.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    ///     Gets the number of grants deactivated by automatic expiration processing
    ///     (<see cref="PermissionOperationType.Expire"/> audit entries).
    /// </summary>
    public int ExpirationsProcessed { get; init; }

    /// <summary>
    ///     Gets the number of manual expiration mutations (bulk set/extend/clear operations on
    ///     tenant permission grants, reconstructed from their old→new expiration audit values).
    /// </summary>
    public int ExpirationMutations { get; init; }

    /// <summary>
    ///     Gets the number of mutations that cleared an expiration (grant made permanent).
    /// </summary>
    public int ExpirationsCleared { get; init; }
}

/// <summary>
///     The combined expiration analytics report (issue #331 "expiration analytics and
///     reporting"): dashboard, trends, and top expiring permissions for a period.
/// </summary>
public sealed record PermissionExpirationReport
{
    /// <summary>
    ///     Gets the tenant the report covers.
    /// </summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    ///     Gets the inclusive report period start (UTC).
    /// </summary>
    public required DateTime PeriodStart { get; init; }

    /// <summary>
    ///     Gets the inclusive report period end (UTC).
    /// </summary>
    public required DateTime PeriodEnd { get; init; }

    /// <summary>
    ///     Gets the current-state dashboard.
    /// </summary>
    public required PermissionExpirationSummary Summary { get; init; }

    /// <summary>
    ///     Gets the daily expiration funnel across the report period.
    /// </summary>
    public required IReadOnlyList<PermissionExpirationTrendPoint> Trends { get; init; }

    /// <summary>
    ///     Gets the total grants durably expired during the period.
    /// </summary>
    public int TotalExpirationsProcessed => Trends.Sum(t => t.ExpirationsProcessed);

    /// <summary>
    ///     Gets the total manual expiration mutations applied during the period.
    /// </summary>
    public int TotalExpirationMutations => Trends.Sum(t => t.ExpirationMutations);

    /// <summary>
    ///     Gets operational health flags derived from the dashboard (empty when healthy).
    ///     Example: expired grants still pending background processing.
    /// </summary>
    public required IReadOnlyList<string> AttentionFlags { get; init; }
}

/// <summary>
///     Expiration analytics and reporting (issue #331): dashboard summaries of the grant
///     population by time-to-expiry, daily expiration-funnel trends reconstructed from the
///     permission audit log, and combined reports.
/// </summary>
public interface IPermissionExpirationAnalyticsService
{
    /// <summary>
    ///     Computes the expiration dashboard summary for one tenant.
    /// </summary>
    Task<PermissionExpirationSummary> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Computes the daily expiration funnel for one tenant across an inclusive period,
    ///     reconstructed from permission audit entries.
    /// </summary>
    Task<List<PermissionExpirationTrendPoint>> GetTrendsAsync(
        Guid tenantId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Produces the combined expiration report (summary + trends + attention flags) for
    ///     one tenant across an inclusive period.
    /// </summary>
    Task<PermissionExpirationReport> GetReportAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);
}
