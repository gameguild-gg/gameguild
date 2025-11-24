using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to add a member to a tenant
/// </summary>
public abstract record AddTenantMemberCommand(Guid TenantId, Guid UserId, string Role, string? InvitedByEmail = null) : ICommand<AddTenantMemberResponse>;
