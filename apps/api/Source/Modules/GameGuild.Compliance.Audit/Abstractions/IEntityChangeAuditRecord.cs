namespace GameGuild.Compliance.Audit;

/// <summary>
///     Centralized-audit contract implemented by the host's universal entity-change audit record
///     (issue #166). The audit module cannot reference the host's entity type directly, so the PII
///     redaction pipeline and the centralized query surface operate through this abstraction: every
///     automatically captured entity modification flows through the same redaction and query
///     infrastructure as the central <see cref="AuditLog"/> store.
/// </summary>
public interface IEntityChangeAuditRecord
{
    /// <summary>Identifier of the captured change record.</summary>
    Guid Id { get; }

    /// <summary>CLR type name of the changed entity.</summary>
    string EntityName { get; }

    /// <summary>Tenant the changed entity belongs to, when tenant-scoped.</summary>
    Guid? TenantId { get; }

    /// <summary>JSON property/value snapshot taken before the change; null for additions.</summary>
    string? BeforeValues { get; set; }

    /// <summary>JSON property/value snapshot taken after the change; null for deletions.</summary>
    string? AfterValues { get; set; }
}

/// <summary>Read model over the universal entity-change audit store, served by the host.</summary>
public sealed record EntityChangeAuditEntry(
    Guid Id,
    string EntityName,
    string KeyId,
    string ChangeKind,
    string? BeforeValues,
    string? AfterValues,
    Guid? TenantId,
    Guid? ActorId,
    string? CorrelationId,
    DateTimeOffset OccurredAtUtc);

/// <summary>Filter for querying the universal entity-change audit store.</summary>
public sealed record EntityChangeAuditQuery(
    Guid? TenantId = null,
    string? EntityName = null,
    string? ChangeKind = null,
    Guid? ActorId = null,
    int Skip = 0,
    int Take = 50);

/// <summary>
///     Host-backed reader over the universal entity-change audit records. Implemented in the API
///     composition root because the record entity lives in the host's database project; the audit
///     module consumes it so captured changes are served through the centralized audit surface.
/// </summary>
public interface IEntityChangeAuditStore
{
    /// <summary>Returns captured entity-change records for one tenant, newest first.</summary>
    Task<IReadOnlyList<EntityChangeAuditEntry>> QueryAsync(EntityChangeAuditQuery query, CancellationToken cancellationToken);

    /// <summary>Number of captured entity-change records for one tenant.</summary>
    Task<int> CountAsync(EntityChangeAuditQuery query, CancellationToken cancellationToken);
}
