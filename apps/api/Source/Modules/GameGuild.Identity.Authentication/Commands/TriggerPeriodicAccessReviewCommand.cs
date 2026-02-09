using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record TriggerPeriodicAccessReviewCommand : ICommand<AccessReviewCampaign>
{
    public Guid ReviewId { get; init; }
}
