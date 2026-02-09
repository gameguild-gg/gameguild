using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetConditionalPolicyEvaluationHistoryQuery : IQuery<ConditionalPolicyEvaluationHistoryDto>
{
    public Guid PolicyId { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}
