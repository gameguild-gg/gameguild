using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Command to archive a tenant (distinct from delete)
/// </summary>
public record ArchiveTenantCommand(Guid TenantId, string? Reason = null) : IRequest<Result<TenantArchiveRecord>>;

/// <summary>
///     Command to unarchive/restore a tenant from archived state
/// </summary>
public record UnarchiveTenantCommand(Guid TenantId) : IRequest<Result<bool>>;

/// <summary>
///     Archive record for tracking tenant archival
/// </summary>
public record TenantArchiveRecord(
    Guid TenantId,
    DateTime ArchivedAt,
    string? Reason,
    Guid ArchivedByUserId);