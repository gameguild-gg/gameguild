using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to activate a tenant
/// </summary>
public record ActivateTenantCommand(Guid TenantId) : ICommand<ActivateTenantResponse>;
