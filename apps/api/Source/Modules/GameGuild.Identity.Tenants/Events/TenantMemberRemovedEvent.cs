using GameGuild.CQRS;
using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Domain event raised when a member is removed from a tenant
/// </summary>
public class TenantMemberRemovedEvent(Guid tenantId, Guid memberId, string memberEmail, string reason) : DomainEvent
{
    public TenantId TenantId { get; } = tenantId;

    public Guid MemberId { get; } = memberId;

    public string MemberEmail { get; } = memberEmail;

    public string Reason { get; } = reason;
}
