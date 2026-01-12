using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record GetConditionalPoliciesQuery : IQuery<Models.PagedResult<ConditionalPolicy>>
{
    public Guid? TenantId { get; init; }

    public bool? IsActive { get; init; }

    public string? ConditionType { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
