using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAbacPoliciesQuery : IQuery<PagedResult<AbacPolicy>>
{
    public Guid? TenantId { get; init; }

    public bool? IsActive { get; init; }

    public string? Category { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
