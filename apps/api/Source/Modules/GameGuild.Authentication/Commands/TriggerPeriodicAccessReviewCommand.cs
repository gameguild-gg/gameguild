using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record TriggerPeriodicAccessReviewCommand : ICommand<AccessReviewCampaign>
{
    public Guid ReviewId { get; init; }
}
