using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record StartAccessReviewCampaignCommand : ICommand<bool>
{
    public Guid CampaignId { get; init; }
}
