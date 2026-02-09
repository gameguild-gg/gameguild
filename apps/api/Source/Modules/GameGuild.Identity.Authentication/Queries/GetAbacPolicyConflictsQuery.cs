using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetAbacPolicyConflictsQuery : IQuery<AbacPolicyConflictsDto>
{
    public Guid? TenantId { get; init; }
}
