using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetAbacPolicyUsageQuery : IQuery<AbacPolicyUsageDto>
{
    public Guid PolicyId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
