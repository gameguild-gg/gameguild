using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetConditionalPolicyUsageQuery : IQuery<ConditionalPolicyUsageDto>
{
    public Guid PolicyId { get; init; }

    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }
}
