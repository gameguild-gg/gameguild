using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed record SetTenantMembershipStatusCommand(
    Guid TenantId,
    Guid UserId,
    bool IsActive,
    string? Reason = null) : ICommand<SetTenantMembershipStatusResponse>;

public sealed record SetTenantMembershipStatusResponse
{
    public bool Success { get; init; }
    public bool NotFound { get; init; }
    public string? Message { get; init; }
    public Guid MemberId { get; init; }
    public bool IsActive { get; init; }
}
