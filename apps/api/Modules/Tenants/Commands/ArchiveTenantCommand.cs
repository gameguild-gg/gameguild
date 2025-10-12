using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Command to archive a tenant (distinct from delete)
/// </summary>
public record ArchiveTenantCommand(Guid TenantId, Guid ArchivedBy, string? Reason = null) : ICommand<Result<bool>>;

/// <summary>
///     Command to unarchive/restore a tenant from archived state
/// </summary>
public record UnarchiveTenantCommand(Guid TenantId) : ICommand<Result<bool>>;

/// <summary>
///     Archive record DTO for tracking tenant archival
/// </summary>
public record TenantArchiveDto(
    Guid TenantId,
    DateTime ArchivedAt,
    string? Reason,
    Guid ArchivedByUserId);
