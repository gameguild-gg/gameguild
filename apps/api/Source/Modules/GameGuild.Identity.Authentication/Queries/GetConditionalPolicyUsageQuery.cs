using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetConditionalPolicyUsageQuery : IQuery<ConditionalPolicyUsageDto>
{
    public Guid PolicyId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
