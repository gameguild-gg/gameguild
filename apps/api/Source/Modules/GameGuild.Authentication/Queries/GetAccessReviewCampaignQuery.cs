using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

// Access Review Campaign Queries
public record GetAccessReviewCampaignQuery : IQuery<AccessReviewCampaign>
{
    public Guid CampaignId { get; init; }
}

// Access Review Item Queries

// Periodic Access Review Queries

// Access Revocation Queries

// Analytics and Compliance Queries

// DTOs for Query Results
