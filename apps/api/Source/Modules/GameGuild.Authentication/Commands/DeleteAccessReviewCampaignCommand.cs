using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record DeleteAccessReviewCampaignCommand : ICommand<bool>
{
    public Guid CampaignId { get; init; }
}
