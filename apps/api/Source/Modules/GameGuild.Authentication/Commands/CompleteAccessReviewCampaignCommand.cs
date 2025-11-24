using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record CompleteAccessReviewCampaignCommand : ICommand<AccessReviewCampaignResult>
{
    public Guid CampaignId { get; init; }
}
