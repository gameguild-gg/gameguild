using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// Access Review Queries
// ============================================================================

/// <summary>
///     Query to get an access review campaign by ID
/// </summary>
public record GetAccessReviewCampaignByIdQuery(Guid CampaignId) : IQuery<AccessReviewCampaign?>;

public class GetAccessReviewCampaignByIdHandler(IAccessReviewService service)
    : IQueryHandler<GetAccessReviewCampaignByIdQuery, AccessReviewCampaign?>
{
    public async Task<AccessReviewCampaign?> Handle(
        GetAccessReviewCampaignByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetCampaignByIdAsync(request.CampaignId, cancellationToken);
    }
}

/// <summary>
///     Query to get active access review campaigns
/// </summary>
public record GetActiveAccessReviewCampaignsQuery(Guid? TenantId) : IQuery<List<AccessReviewCampaign>>;

public class GetActiveAccessReviewCampaignsHandler(IAccessReviewService service)
    : IQueryHandler<GetActiveAccessReviewCampaignsQuery, List<AccessReviewCampaign>>
{
    public async Task<List<AccessReviewCampaign>> Handle(
        GetActiveAccessReviewCampaignsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetActiveCampaignsAsync(request.TenantId, cancellationToken);
    }
}

/// <summary>
///     Query to get pending review items for a reviewer
/// </summary>
public record GetPendingReviewItemsQuery(Guid ReviewerId, Guid? TenantId) : IQuery<List<AccessReviewItem>>;

public class GetPendingReviewItemsHandler(IAccessReviewService service)
    : IQueryHandler<GetPendingReviewItemsQuery, List<AccessReviewItem>>
{
    public async Task<List<AccessReviewItem>> Handle(
        GetPendingReviewItemsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetPendingItemsForReviewerAsync(request.ReviewerId, request.TenantId, cancellationToken);
    }
}
