using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetConditionalPolicyConflictsQuery : IQuery<ConditionalPolicyConflictsDto>
{
    public Guid? TenantId { get; init; }
}
