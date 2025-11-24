using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetPeriodicAccessReviewsQuery : IQuery<PagedResult<PeriodicAccessReview>>
{
    public Guid? TenantId { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
