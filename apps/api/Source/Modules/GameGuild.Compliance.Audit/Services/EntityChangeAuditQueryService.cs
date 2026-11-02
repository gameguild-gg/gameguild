using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>Filter accepted by the entity-change audit query endpoint.</summary>
public sealed record EntityChangeAuditListRequest
{
    /// <summary>Filter by entity CLR name (for example <c>Project</c>).</summary>
    public string? EntityName { get; init; }

    /// <summary>Filter by change kind: <c>Added</c>, <c>Modified</c>, or <c>Deleted</c>.</summary>
    public string? ChangeKind { get; init; }

    /// <summary>Filter by the actor whose unit of work produced the change.</summary>
    public Guid? ActorId { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; } = 50;
}

/// <summary>One page of universal entity-change audit records plus the total for the filter.</summary>
public sealed record EntityChangeAuditListResponse(
    IReadOnlyList<EntityChangeAuditEntry> Changes,
    int TotalCount);

public interface IEntityChangeAuditQueryService
{
    /// <summary>
    ///     Returns the universal entity-change audit history for the current tenant. Requires an
    ///     authenticated tenant administrator; records never cross tenant boundaries.
    /// </summary>
    Task<EntityChangeAuditListResponse> ListForCurrentTenantAsync(EntityChangeAuditListRequest request, CancellationToken cancellationToken);
}

/// <summary>
///     Serves the automatically captured entity-change history (issue #166) through the centralized
///     audit surface. Reads are tenant-scoped and restricted to tenant administrators: the records
///     carry before/after deltas of every audited entity, so they are at least as sensitive as the
///     central audit log.
/// </summary>
public sealed class EntityChangeAuditQueryService(
    IActorContextAccessor actors,
    IEntityChangeAuditStore store,
    ILogger<EntityChangeAuditQueryService> logger) : IEntityChangeAuditQueryService
{
    public async Task<EntityChangeAuditListResponse> ListForCurrentTenantAsync(EntityChangeAuditListRequest request, CancellationToken cancellationToken)
    {
        var actor = actors.ActorContext;
        if (!actor.IsAuthenticated || !actor.IsTenantAdmin || actor.TenantId is null || actor.TenantId == Guid.Empty)
        {
            logger.LogWarning("Entity-change audit query denied for actor {Actor}", actor.SubjectIdAsGuid);
            throw new UnauthorizedAccessException("A tenant administrator and tenant context are required.");
        }

        if (request.Skip < 0 || request.Take is < 1 or > 200)
        {
            throw new SecurityLogRetentionValidationException(new Dictionary<string, string[]>
            {
                ["Pagination"] = ["Use skip >= 0 and 1 <= take <= 200."]
            });
        }

        var query = new EntityChangeAuditQuery(
            actor.TenantId,
            string.IsNullOrWhiteSpace(request.EntityName) ? null : request.EntityName.Trim(),
            string.IsNullOrWhiteSpace(request.ChangeKind) ? null : request.ChangeKind.Trim(),
            request.ActorId,
            request.Skip,
            request.Take);

        var changes = await store.QueryAsync(query, cancellationToken).ConfigureAwait(false);
        var totalCount = await store.CountAsync(query, cancellationToken).ConfigureAwait(false);
        return new EntityChangeAuditListResponse(changes, totalCount);
    }
}
