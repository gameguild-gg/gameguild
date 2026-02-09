using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetPeriodicAccessReviewsQuery : IQuery<PagedResult<PeriodicAccessReview>>
{
    public Guid? TenantId { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
