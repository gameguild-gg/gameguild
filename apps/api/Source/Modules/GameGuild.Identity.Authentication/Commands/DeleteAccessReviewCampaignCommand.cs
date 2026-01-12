using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record DeleteAccessReviewCampaignCommand : ICommand<bool>
{
    public Guid CampaignId { get; init; }
}
