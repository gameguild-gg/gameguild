using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Identity.Authentication;

// Access Review Campaign Commands
[RequiresQuota(ResourceUsageType.AccessReviewCampaigns, 1, Source = "CreateAccessReviewCampaign")]
public record CreateAccessReviewCampaignCommand : ICommand<AccessReviewCampaign>
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid TenantId { get; init; }

    public string ReviewType { get; init; } = string.Empty;

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public List<Guid> ReviewerIds { get; init; } = new List<Guid>();

    public List<string> Scopes { get; init; } = new List<string>();
}

// Access Review Item Commands

// Periodic Access Review Commands

// Access Revocation Commands

// Report and Analytics Commands

// Result DTOs
