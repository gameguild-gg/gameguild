using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record StartAccessReviewCampaignCommand : ICommand<bool>
{
    public Guid CampaignId { get; init; }
}
