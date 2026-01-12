using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAbacPolicyUsageQuery : IQuery<AbacPolicyUsageDto>
{
    public Guid PolicyId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
