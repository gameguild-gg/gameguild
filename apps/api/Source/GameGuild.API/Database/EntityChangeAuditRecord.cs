using GameGuild.Compliance.Audit;

namespace GameGuild.API.Database;

/// <summary>
///     Insert-only record of a single automatically captured entity change, written in
///     the same <c>SaveChanges</c> call as the change itself so a rolled-back
///     transaction leaves no audit rows. Audit-store entities (including this type) are
///     excluded from capture, so writing audit rows never triggers audit-of-audit
///     recursion. Sensitive properties are stored with their value redacted, and the
///     persisted snapshots additionally pass through the centralized PII redaction
///     pipeline (<see cref="AuditLogPiiRedactionInterceptor"/>) like every other audit
///     sink. Implements the module-facing contract
///     <see cref="IEntityChangeAuditRecord"/> so the compliance audit module can serve
///     and redact these records without referencing the host entity type.
/// </summary>
public sealed class EntityChangeAuditRecord : IEntityChangeAuditRecord
{
    public Guid Id { get; set; }

    /// <summary>CLR type name of the changed entity.</summary>
    public required string EntityName { get; set; }

    /// <summary>Primary key value(s) of the changed entity, colon-joined for composite keys.</summary>
    public required string KeyId { get; set; }

    /// <summary>EF change kind: Added, Modified, or Deleted.</summary>
    public required string ChangeKind { get; set; }

    /// <summary>JSON property/values snapshot taken before the change (null for Added).</summary>
    public string? BeforeValues { get; set; }

    /// <summary>JSON property/values snapshot taken after the change (null for Deleted).</summary>
    public string? AfterValues { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? ActorId { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
