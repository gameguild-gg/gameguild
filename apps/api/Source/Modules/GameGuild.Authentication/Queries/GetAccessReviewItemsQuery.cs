using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetAccessReviewItemsQuery : IQuery<PagedResult<AccessReviewItem>>
{
    public Guid CampaignId { get; init; }

    public string? Status { get; init; }

    public Guid? ReviewerId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
