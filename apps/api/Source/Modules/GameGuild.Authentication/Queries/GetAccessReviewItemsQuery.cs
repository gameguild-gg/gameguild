using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record GetAccessReviewItemsQuery : IQuery<Models.PagedResult<AccessReviewItem>>
{
    public Guid CampaignId { get; init; }

    public string? Status { get; init; }

    public Guid? ReviewerId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
