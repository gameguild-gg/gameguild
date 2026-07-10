using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to add a member to a tenant
/// </summary>
public record AddTenantMemberCommand(
    Guid TenantId,
    Guid UserId,
    string Role,
    string? InvitedByEmail = null,
    bool RequiresAcceptance = false,
    string? InviteeEmail = null,
    string? InviteeName = null) : ICommand<AddTenantMemberResponse>;
