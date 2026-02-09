using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record DeleteAccessReviewCampaignCommand : ICommand<bool>
{
    public Guid CampaignId { get; init; }
}
