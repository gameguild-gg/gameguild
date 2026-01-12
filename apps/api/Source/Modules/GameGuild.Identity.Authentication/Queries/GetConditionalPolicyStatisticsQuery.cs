using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetConditionalPolicyStatisticsQuery : IQuery<ConditionalPolicyStatisticsDto>
{
    public Guid? TenantId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
