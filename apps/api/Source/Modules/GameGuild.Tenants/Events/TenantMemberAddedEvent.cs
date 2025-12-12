using GameGuild.CQRS;

namespace GameGuild.Tenants.Events;

/// <summary>
///     Domain event raised when a member is added to a tenant
/// </summary>
public class TenantMemberAddedEvent(Guid tenantId, Guid memberId, string memberEmail, string role) : DomainEvent
{
    public TenantId TenantId { get; } = tenantId;

    public Guid MemberId { get; } = memberId;

    public string MemberEmail { get; } = memberEmail;

    public string Role { get; } = role;
}
