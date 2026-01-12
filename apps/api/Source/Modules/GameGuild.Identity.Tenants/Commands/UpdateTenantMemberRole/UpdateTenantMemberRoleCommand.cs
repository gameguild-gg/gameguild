using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to update a tenant member's role
/// </summary>
public abstract record UpdateTenantMemberRoleCommand(Guid TenantId, Guid UserId, string NewRole) : ICommand<UpdateTenantMemberRoleResponse>;
