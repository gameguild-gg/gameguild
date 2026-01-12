using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record UpdateAccessReviewCampaignCommand : ICommand<AccessReviewCampaign>
{
    public Guid CampaignId { get; set; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public List<Guid>? ReviewerIds { get; init; }
}
