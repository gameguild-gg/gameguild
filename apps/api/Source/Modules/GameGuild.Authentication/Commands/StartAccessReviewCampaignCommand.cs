using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record StartAccessReviewCampaignCommand : ICommand<bool>
{
    public Guid CampaignId { get; init; }
}
