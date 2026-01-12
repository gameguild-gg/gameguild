using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to delete a tenant (soft or hard delete)
/// </summary>
public record DeleteTenantCommand(Guid TenantId, bool HardDelete = false, string? Reason = null) : ICommand;
