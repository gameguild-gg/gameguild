using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetConditionalPolicyConflictsQuery : IQuery<ConditionalPolicyConflictsDto>
{
    public Guid? TenantId { get; init; }
}
