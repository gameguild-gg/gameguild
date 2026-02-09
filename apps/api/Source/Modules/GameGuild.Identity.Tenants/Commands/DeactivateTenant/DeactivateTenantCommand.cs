using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to deactivate a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public sealed record DeactivateTenantCommand(Guid TenantId) : ICommand;
