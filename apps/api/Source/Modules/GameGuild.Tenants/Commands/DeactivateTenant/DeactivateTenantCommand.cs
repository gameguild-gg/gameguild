using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to deactivate a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public record DeactivateTenantCommand(Guid TenantId) : ICommand;
