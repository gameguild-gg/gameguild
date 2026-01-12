using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record CompleteAccessReviewCampaignCommand : ICommand<AccessReviewCampaignResult>
{
    public Guid CampaignId { get; init; }
}
