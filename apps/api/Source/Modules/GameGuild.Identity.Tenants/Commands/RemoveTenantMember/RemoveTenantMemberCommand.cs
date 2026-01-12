using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to remove a member from a tenant
/// </summary>
public abstract record RemoveTenantMemberCommand(Guid TenantId, Guid UserId) : ICommand<RemoveTenantMemberResponse>;
