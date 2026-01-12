using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAbacPolicyConflictsQuery : IQuery<AbacPolicyConflictsDto>
{
    public Guid? TenantId { get; init; }
}
