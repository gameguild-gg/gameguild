using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetAccessReviewCampaignsQuery : IQuery<PagedResult<AccessReviewCampaign>>
{
    public Guid? TenantId { get; init; }

    public string? Status { get; init; }

    public string? Type { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
