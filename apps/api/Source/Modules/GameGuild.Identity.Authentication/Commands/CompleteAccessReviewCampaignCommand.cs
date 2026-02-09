using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record CompleteAccessReviewCampaignCommand : ICommand<AccessReviewCampaignResult>
{
    public Guid CampaignId { get; init; }
}
