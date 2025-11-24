using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetConditionalPolicyConflictsQuery : IQuery<ConditionalPolicyConflictsDto>
{
    public Guid? TenantId { get; init; }
}
