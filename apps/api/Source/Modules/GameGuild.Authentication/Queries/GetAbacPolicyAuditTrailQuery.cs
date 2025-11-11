using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetAbacPolicyAuditTrailQuery : IQuery<AbacPolicyAuditTrailDto>
{
    public Guid PolicyId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
