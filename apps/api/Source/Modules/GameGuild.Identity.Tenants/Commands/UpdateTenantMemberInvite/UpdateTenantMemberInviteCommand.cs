using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public enum TenantMemberInviteAction
{
    Resend = 1,
    Cancel = 2,
    Accept = 3
}

public sealed record UpdateTenantMemberInviteCommand(
    Guid TenantId,
    Guid UserId,
    TenantMemberInviteAction Action,
    string? ActorEmail = null) : ICommand<UpdateTenantMemberInviteResponse>;

public sealed record UpdateTenantMemberInviteResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Guid? MemberId { get; init; }

    public string? InviteStatus { get; init; }
}
