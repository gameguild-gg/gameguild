using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// Permission Analytics Queries
// ============================================================================

/// <summary>
///     Query to get permission usage metrics
/// </summary>
public sealed record GetPermissionUsageQuery(
    Guid? TenantId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IQuery<List<PermissionUsageMetrics>>;

public sealed class GetPermissionUsageHandler(IPermissionAnalyticsService service)
    : IQueryHandler<GetPermissionUsageQuery, List<PermissionUsageMetrics>>
{
    public async Task<List<PermissionUsageMetrics>> Handle(
        GetPermissionUsageQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetPermissionUsageAsync(
            request.TenantId,
            request.FromDate,
            request.ToDate,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get user activity summary
/// </summary>
public sealed record GetUserActivityQuery(
    Guid? TenantId,
    int Top = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IQuery<List<UserActivitySummary>>;

public sealed class GetUserActivityHandler(IPermissionAnalyticsService service)
    : IQueryHandler<GetUserActivityQuery, List<UserActivitySummary>>
{
    public async Task<List<UserActivitySummary>> Handle(
        GetUserActivityQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetUserActivityAsync(
            request.TenantId,
            request.Top,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );
    }
}

/// <summary>
///     Query to get resource access patterns
/// </summary>
public sealed record GetResourceAccessPatternsQuery(
    Guid? TenantId,
    int Top = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IQuery<List<ResourceAccessPattern>>;

public sealed class GetResourceAccessPatternsHandler(IPermissionAnalyticsService service)
    : IQueryHandler<GetResourceAccessPatternsQuery, List<ResourceAccessPattern>>
{
    public async Task<List<ResourceAccessPattern>> Handle(
        GetResourceAccessPatternsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetResourceAccessPatternsAsync(
            request.TenantId,
            request.Top,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );
    }
}

/// <summary>
///     Query to get permission trends
/// </summary>
public sealed record GetPermissionTrendsQuery(
    Guid? TenantId,
    DateTime FromDate,
    DateTime ToDate
) : IQuery<List<PermissionTrend>>;

public sealed class GetPermissionTrendsHandler(IPermissionAnalyticsService service)
    : IQueryHandler<GetPermissionTrendsQuery, List<PermissionTrend>>
{
    public async Task<List<PermissionTrend>> Handle(
        GetPermissionTrendsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetPermissionTrendsAsync(
            request.TenantId,
            request.FromDate,
            request.ToDate,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to detect permission anomalies
/// </summary>
public sealed record DetectPermissionAnomaliesQuery(
    Guid? TenantId,
    DateTime? FromDate = null
) : IQuery<List<PermissionAnomaly>>;

public sealed class DetectPermissionAnomaliesHandler(IPermissionAnalyticsService service)
    : IQueryHandler<DetectPermissionAnomaliesQuery, List<PermissionAnomaly>>
{
    public async Task<List<PermissionAnomaly>> Handle(
        DetectPermissionAnomaliesQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.DetectAnomaliesAsync(
            request.TenantId,
            request.FromDate,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to generate a permission analytics report
/// </summary>
public sealed record GeneratePermissionReportQuery(
    Guid? TenantId,
    DateTime PeriodStart,
    DateTime PeriodEnd
) : IQuery<PermissionAnalyticsReport>;

public sealed class GeneratePermissionReportHandler(IPermissionAnalyticsService service)
    : IQueryHandler<GeneratePermissionReportQuery, PermissionAnalyticsReport>
{
    public async Task<PermissionAnalyticsReport> Handle(
        GeneratePermissionReportQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GenerateReportAsync(
            request.TenantId,
            request.PeriodStart,
            request.PeriodEnd,
            cancellationToken
        ).ConfigureAwait(false);
    }
}
