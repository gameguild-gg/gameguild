using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to activate a tenant
/// </summary>
public sealed record ActivateTenantCommand(Guid TenantId) : ICommand<ActivateTenantResponse>;
