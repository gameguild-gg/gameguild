using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Context.Actors;

using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// Permission Expiration Analytics Queries (issue #331)
// ============================================================================

/// <summary>
///     Query for the tenant expiration dashboard (issue #331 "expiration analytics and
///     reporting"): the grant population bucketed by time-to-expiry plus the per-permission
///     breakdown of grants expiring within the analytics window.
/// </summary>
public sealed record GetExpirationSummaryQuery : IQuery<PermissionExpirationSummary>
{
    /// <summary>
    ///     Gets the tenant the summary covers.
    /// </summary>
    public required TenantId TenantId { get; init; }
}

/// <summary>
///     Handler for GetExpirationSummaryQuery.
/// </summary>
public sealed class GetExpirationSummaryQueryHandler(
    IPermissionExpirationAnalyticsService analyticsService,
    IActorContextAccessor actorContextAccessor,
    ILogger<GetExpirationSummaryQueryHandler> logger)
    : IQueryHandler<GetExpirationSummaryQuery, PermissionExpirationSummary>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PermissionExpirationSummary> Handle(GetExpirationSummaryQuery request, CancellationToken cancellationToken)
    {
        PermissionExpirationCommandGuards.EnsureCanManageTenant(Actor, request.TenantId.Value, "view expiration analytics");

        logger.LogInformation("Computing expiration summary for tenant {TenantId}", request.TenantId);

        return await analyticsService
            .GetSummaryAsync(request.TenantId.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
///     Query for the daily expiration funnel of a tenant across an inclusive period
///     (automatic expirations processed and manual expiration mutations applied per day).
/// </summary>
public sealed record GetExpirationTrendsQuery : IQuery<List<PermissionExpirationTrendPoint>>
{
    /// <summary>
    ///     Gets the tenant the trends cover.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the inclusive period start (UTC).
    /// </summary>
    public required DateTime FromDate { get; init; }

    /// <summary>
    ///     Gets the inclusive period end (UTC).
    /// </summary>
    public required DateTime ToDate { get; init; }
}

/// <summary>
///     Handler for GetExpirationTrendsQuery.
/// </summary>
public sealed class GetExpirationTrendsQueryHandler(
    IPermissionExpirationAnalyticsService analyticsService,
    IActorContextAccessor actorContextAccessor,
    ILogger<GetExpirationTrendsQueryHandler> logger)
    : IQueryHandler<GetExpirationTrendsQuery, List<PermissionExpirationTrendPoint>>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<List<PermissionExpirationTrendPoint>> Handle(GetExpirationTrendsQuery request, CancellationToken cancellationToken)
    {
        PermissionExpirationCommandGuards.EnsureCanManageTenant(Actor, request.TenantId.Value, "view expiration trends");

        logger.LogInformation(
            "Computing expiration trends for tenant {TenantId} from {FromDate} to {ToDate}",
            request.TenantId,
            request.FromDate,
            request.ToDate);

        return await analyticsService
            .GetTrendsAsync(request.TenantId.Value, request.FromDate, request.ToDate, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
///     Query for the combined expiration report (dashboard + funnel trends + attention flags)
///     of a tenant across an inclusive period.
/// </summary>
public sealed record GetExpirationReportQuery : IQuery<PermissionExpirationReport>
{
    /// <summary>
    ///     Gets the tenant the report covers.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the inclusive report period start (UTC).
    /// </summary>
    public required DateTime PeriodStart { get; init; }

    /// <summary>
    ///     Gets the inclusive report period end (UTC).
    /// </summary>
    public required DateTime PeriodEnd { get; init; }
}

/// <summary>
///     Handler for GetExpirationReportQuery.
/// </summary>
public sealed class GetExpirationReportQueryHandler(
    IPermissionExpirationAnalyticsService analyticsService,
    IActorContextAccessor actorContextAccessor,
    ILogger<GetExpirationReportQueryHandler> logger)
    : IQueryHandler<GetExpirationReportQuery, PermissionExpirationReport>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PermissionExpirationReport> Handle(GetExpirationReportQuery request, CancellationToken cancellationToken)
    {
        PermissionExpirationCommandGuards.EnsureCanManageTenant(Actor, request.TenantId.Value, "view expiration reports");

        logger.LogInformation(
            "Generating expiration report for tenant {TenantId} from {PeriodStart} to {PeriodEnd}",
            request.TenantId,
            request.PeriodStart,
            request.PeriodEnd);

        return await analyticsService
            .GetReportAsync(request.TenantId.Value, request.PeriodStart, request.PeriodEnd, cancellationToken)
            .ConfigureAwait(false);
    }
}
