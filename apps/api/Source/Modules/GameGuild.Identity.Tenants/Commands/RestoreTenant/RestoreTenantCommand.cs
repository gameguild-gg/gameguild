using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to restore (unarchive) a tenant
/// </summary>
public abstract record RestoreTenantCommand(Guid TenantId) : ICommand<RestoreTenantResponse>;
