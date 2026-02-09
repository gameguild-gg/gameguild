using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

// Access Review Campaign Queries
public sealed record GetAccessReviewCampaignQuery : IQuery<AccessReviewCampaign>
{
    public Guid CampaignId { get; init; }
}

// Access Review Item Queries

// Periodic Access Review Queries

// Access Revocation Queries

// Analytics and Compliance Queries

// DTOs for Query Results
