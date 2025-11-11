using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetAbacPolicyConflictsQuery : IQuery<AbacPolicyConflictsDto>
{
    public Guid? TenantId { get; init; }
}
