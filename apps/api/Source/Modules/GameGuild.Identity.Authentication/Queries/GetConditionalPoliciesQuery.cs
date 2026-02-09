using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetConditionalPoliciesQuery : IQuery<PagedResult<ConditionalPolicy>>
{
    public Guid? TenantId { get; init; }

    public bool? IsActive { get; init; }

    public string? ConditionType { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
