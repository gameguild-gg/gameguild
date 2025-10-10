using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Command to archive a tenant (distinct from delete)
/// </summary>
public record ArchiveTenantCommand(Guid TenantId, string? Reason = null) : ICommand<Result>;

/// <summary>
///     Command to unarchive/restore a tenant from archived state
/// </summary>
public record UnarchiveTenantCommand(Guid TenantId) : ICommand<Result>;