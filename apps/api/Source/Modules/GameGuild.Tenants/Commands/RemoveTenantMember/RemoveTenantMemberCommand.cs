using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to remove a member from a tenant
/// </summary>
public abstract record RemoveTenantMemberCommand(Guid TenantId, Guid UserId) : ICommand<RemoveTenantMemberResponse>;
