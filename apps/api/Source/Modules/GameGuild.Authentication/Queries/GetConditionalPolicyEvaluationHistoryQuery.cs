using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetConditionalPolicyEvaluationHistoryQuery : IQuery<ConditionalPolicyEvaluationHistoryDto>
{
    public Guid PolicyId { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}
