using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// ============================================================================
// PERMISSION ADMIN QUERY HANDLERS (issue #1278)
//
// GET /v1/permissions/audit-trail and GET /v1/tenants/{tenantId}/permissions/analytics
// (PermissionAdminController) dispatch GetPermissionAuditTrailQuery and
// GetPermissionAnalyticsQuery. The records existed without handlers, so both
// endpoints failed at dispatch time with a 500 (the E9 dead-handler class).
// These handlers back them with the Authorization module's PermissionAuditLog
// store — the same rows every permission mutation writes.
//
// Guards (fail-closed, same policy as PermissionEvaluationQueryHandlers): the
// actor must be authenticated; a non-system actor is restricted to their own
// tenant; reading another user's audit entries requires system administration,
// tenant administration or user-read.
// ============================================================================

/// <summary>
///     Compliance-oriented, paginated view of the permission audit log.
/// </summary>
public sealed class GetPermissionAuditTrailQueryHandler(
    IApplicationDbContext dbContext,
    IActorContextAccessor actorContextAccessor,
    ILogger<GetPermissionAuditTrailQueryHandler> logger)
    : IQueryHandler<GetPermissionAuditTrailQuery, PermissionAuditTrailDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PermissionAuditTrailDto> Handle(GetPermissionAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var tenantScope = PermissionEvaluationQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        PermissionEvaluationQueryGuards.EnsureCanReadPermissions(Actor, request.UserId ?? Actor.SubjectIdAsGuid ?? Guid.Empty, tenantScope);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var from = request.FromDate ?? SystemClock.UtcNow.AddDays(-30);
        var to = request.ToDate ?? SystemClock.UtcNow;

        var query = dbContext.Set<PermissionAuditLog>().AsNoTracking()
            .Where(log => log.TenantId == new TenantId(tenantScope) && log.Timestamp >= from && log.Timestamp <= to);

        if (request.UserId.HasValue)
        {
            var userId = request.UserId.Value;
            query = query.Where(log => log.UserId == userId || log.PerformedBy == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Narrow server-side projection; the enum/dto mapping runs in memory so the
        // translated query stays a plain filter/sort/page (see SubscriptionRepository).
        var rows = await query
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new
            {
                log.Id,
                log.Timestamp,
                log.OperationType,
                log.UserId,
                log.ResourceId,
                log.ResourceType,
                log.PermissionType,
                log.PerformedBy,
                log.Reason
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<PermissionAuditEntry>(rows.Count);
        foreach (var row in rows)
        {
            // Fail-closed mapping: rows without a catalogued permission type are still
            // reported (compliance trail) with the default (None) value.
            Enum.TryParse<PermissionType>(row.PermissionType, ignoreCase: true, out var permission);

            entries.Add(new ConcretePermissionAuditEntry
            {
                Id = row.Id,
                Timestamp = row.Timestamp,
                Action = row.OperationType.ToString(),
                UserId = row.UserId ?? Guid.Empty,
                Permission = permission,
                Level = string.IsNullOrWhiteSpace(row.ResourceType) ? "Tenant" : row.ResourceType,
                ResourceId = row.ResourceId,
                ContentType = row.ResourceType,
                PerformedBy = row.PerformedBy == Guid.Empty ? null : row.PerformedBy.ToString(),
                Reason = row.Reason
            });
        }

        logger.LogInformation(
            "Permission audit trail: {Count} of {Total} entries for tenant {TenantId} (page {Page}, pageSize {PageSize})",
            entries.Count, totalCount, tenantScope, page, pageSize);

        return new ConcretePermissionAuditTrailDto
        {
            Entries = entries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private sealed class ConcretePermissionAuditTrailDto : PermissionAuditTrailDto;
    private sealed class ConcretePermissionAuditEntry : PermissionAuditEntry;
}

/// <summary>
///     Permission usage analytics for one tenant, aggregated from the audit log.
/// </summary>
public sealed class GetPermissionAnalyticsQueryHandler(
    IApplicationDbContext dbContext,
    IActorContextAccessor actorContextAccessor,
    ILogger<GetPermissionAnalyticsQueryHandler> logger)
    : IQueryHandler<GetPermissionAnalyticsQuery, PermissionAnalyticsDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PermissionAnalyticsDto> Handle(GetPermissionAnalyticsQuery request, CancellationToken cancellationToken)
    {
        PermissionEvaluationQueryGuards.EnsureCanReadPermissions(Actor, Actor.SubjectIdAsGuid ?? Guid.Empty, request.TenantId);

        var from = request.FromDate;
        var to = request.ToDate;

        // Narrow projection of the window; the day/user/permission aggregations run in
        // memory so the translated query stays a plain filter (repo convention).
        var rows = await dbContext.Set<PermissionAuditLog>().AsNoTracking()
            .Where(log => log.TenantId == new TenantId(request.TenantId) && log.Timestamp >= from && log.Timestamp <= to)
            .Select(log => new { log.Timestamp, log.UserId, log.PermissionType })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var usage = new Dictionary<PermissionType, int>();
        foreach (var grouping in rows
                     .Where(row => !string.IsNullOrWhiteSpace(row.PermissionType))
                     .GroupBy(row => row.PermissionType, StringComparer.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<PermissionType>(grouping.Key, ignoreCase: true, out var permission)
                && Enum.IsDefined(permission))
            {
                usage[permission] = grouping.Count();
            }
        }

        var trends = rows
            .GroupBy(row => row.Timestamp.Date)
            .OrderBy(day => day.Key)
            .Select(day => (PermissionTrend)new ConcretePermissionTrend
            {
                Date = day.Key,
                PermissionCount = day.Count(),
                UserCount = day.Select(row => row.UserId).Distinct().Count()
            })
            .ToList();

        var topUsers = rows
            .Where(row => row.UserId.HasValue)
            .GroupBy(row => row.UserId.Value)
            .Select(user => new
            {
                UserId = user.Key,
                PermissionCount = user.Count(),
                LastActivity = user.Max(row => row.Timestamp)
            })
            .OrderByDescending(user => user.PermissionCount)
            .Take(10)
            .Select(user => (TopPermissionUser)new ConcreteTopPermissionUser
            {
                UserId = user.UserId,
                PermissionCount = user.PermissionCount,
                LastActivity = user.LastActivity
            })
            .ToList();

        logger.LogInformation(
            "Permission analytics for tenant {TenantId}: {Rows} audit entries, {PermissionCount} distinct permissions, {UserCount} users",
            request.TenantId, rows.Count, usage.Count, topUsers.Count);

        return new ConcretePermissionAnalyticsDto
        {
            TenantId = request.TenantId,
            FromDate = from,
            ToDate = to,
            TotalUsers = rows.Select(row => row.UserId).Distinct().Count(),
            TotalPermissions = usage.Values.Sum(),
            PermissionUsage = usage,
            UsageTrends = trends,
            TopUsers = topUsers
        };
    }

    private sealed class ConcretePermissionAnalyticsDto : PermissionAnalyticsDto;
    private sealed class ConcretePermissionTrend : PermissionTrend;
    private sealed class ConcreteTopPermissionUser : TopPermissionUser;
}
